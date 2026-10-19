using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.StatHub.Configuration;
using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.Entities.Monitoring;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Aiursoft.StatHub.Services.Monitoring;

public sealed class MonitoringService(StatHubDbContext db, MonitoringCoordinator coordinator,
    IAlertCenterSettingsProvider settingsProvider, IMonitoringIncidentClient client,
    TimeProvider clock, ILogger<MonitoringService> logger)
{
    private const string Source = "StatHub.HealthMonitoring";
    public static readonly TimeSpan OfflineTimeout = TimeSpan.FromMinutes(1);

    public async Task<MonitoringPolicy> PolicyAsync()
    {
        var policy = await db.AlertMonitoringPolicies.Include(item => item.Rules).Include(item => item.Targets)
            .SingleOrDefaultAsync(item => item.Id == MonitoringPolicy.DefaultId);
        if (policy != null) return policy;
        policy = new MonitoringPolicy
        {
            Rules = [Rule("yellow", 35, "sev3"), Rule("yellow", 100, "sev2"),
                Rule("red", 35, "sev2"), Rule("red", 100, "sev1"), Rule("grey", 15, "sev2")]
        };
        db.AlertMonitoringPolicies.Add(policy);
        await db.SaveChangesAsync();
        return policy;
    }

    private static MonitoringRule Rule(string state, int minutes, string severity) =>
        new() { State = state, DurationMinutes = minutes, Severity = severity };

    public static bool Includes(MonitoringPolicy policy, string clientId)
    {
        var target = policy.Targets.SingleOrDefault(item => item.ClientId == clientId);
        return target?.IsExcluded != true && (policy.MonitorAllServers || target != null);
    }

    public static string? Severity(MonitoringPolicy policy, AgentMonitoringState state, DateTime now) =>
        policy.Rules.Where(rule => rule.IsEnabled && rule.State == state.CurrentState &&
                now - state.StateSinceUtc >= TimeSpan.FromMinutes(rule.DurationMinutes))
            .OrderBy(rule => rule.Severity).Select(rule => rule.Severity).FirstOrDefault();

    public async Task ObserveAsync(string clientId, string hostname, IReadOnlyList<string> observations)
    {
        var colors = observations.Where(color => color is "green" or "yellow" or "red").ToArray();
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 200 || colors.Length == 0) return;
        await coordinator.Gate.WaitAsync();
        try
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var policy = await PolicyAsync();
            var state = await db.AgentMonitoringStates.FindAsync(clientId);
            if (state == null)
            {
                state = new AgentMonitoringState { ClientId = clientId, StateSinceUtc = now };
                db.AgentMonitoringStates.Add(state);
            }
            var interrupted = state.ObservationSessionId != coordinator.SessionId || now - state.LastReportedAtUtc > OfflineTimeout;
            foreach (var color in colors)
            {
                if (interrupted || state.CurrentState != color) state.StateSinceUtc = now;
                interrupted = false;
                state.CurrentState = color;
                // Retain even a transient green inside one ten-sample report until recovery succeeds.
                if (color == "green" && policy.IsEnabled && Includes(policy, clientId) && policy.AutoResolveOnGreen)
                    state.PendingRecoveryAtUtc = now;
            }
            state.Hostname = hostname.Length > 200 ? hostname[..200] : hostname;
            state.LastReportedAtUtc = now;
            state.ObservationSessionId = coordinator.SessionId;
            await db.SaveChangesAsync();
            if (state.PendingRecoveryAtUtc.HasValue || Severity(policy, state, now) != null)
                await SynchronizeAsync(policy, [state], now);
        }
        finally { coordinator.Gate.Release(); }
    }

    public async Task CheckAsync()
    {
        await coordinator.Gate.WaitAsync();
        try
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var policy = await PolicyAsync();
            var states = await db.AgentMonitoringStates.ToListAsync();
            foreach (var state in states)
            {
                var offlineAt = state.LastReportedAtUtc + OfflineTimeout;
                if (now >= offlineAt && state.CurrentState != "grey")
                {
                    state.CurrentState = "grey";
                    state.StateSinceUtc = offlineAt;
                }
            }
            await db.SaveChangesAsync();
            await SynchronizeAsync(policy, states.Where(state => state.CurrentState == "grey" ||
                state.ObservationSessionId == coordinator.SessionId || state.PendingRecoveryAtUtc.HasValue).ToList(), now);
        }
        finally { coordinator.Gate.Release(); }
    }

    private async Task SynchronizeAsync(MonitoringPolicy policy, IReadOnlyList<AgentMonitoringState> states, DateTime now)
    {
        try
        {
            var settings = await settingsProvider.GetAsync();
            if (!settings.Enabled || !policy.IsEnabled) return;
            settings.Validate();
            var active = await LoadActiveAsync();
            foreach (var state in states.Where(state => Includes(policy, state.ClientId)))
            {
                try
                {
                    var owned = active.Where(item => item.Context?.Value<string>("source") == Source &&
                        item.Context.Value<string>("clientId") == state.ClientId).ToList();
                    // A successful query also confirms a create whose response was lost.
                    if (owned.Any(item => item.ReportId == state.NextReportId))
                    {
                        state.NextReportId = Guid.NewGuid();
                        await db.SaveChangesAsync();
                    }
                    if (state.PendingRecoveryAtUtc.HasValue)
                    {
                        if (policy.AutoResolveOnGreen)
                        {
                            foreach (var incident in owned.Where(item => item.ReceivedAtUtc.UtcDateTime <= state.PendingRecoveryAtUtc.Value).ToList())
                            {
                                await client.ResolveAsync(incident.IncidentId, new ResolveIncidentRequest
                                {
                                    Version = incident.Version,
                                    Reason = $"StatHub received a healthy sample at {state.PendingRecoveryAtUtc:O}."
                                });
                                owned.Remove(incident);
                            }
                        }
                        state.PendingRecoveryAtUtc = null;
                        await db.SaveChangesAsync();
                    }
                    var severity = Severity(policy, state, now);
                    if (severity == null || (state.CurrentState != "grey" && state.ObservationSessionId != coordinator.SessionId)) continue;
                    if (owned.Count == 0)
                    {
                        var title = $"{state.Hostname}: {state.CurrentState} server health";
                        await client.CreateAsync(new CreateIncidentRequest
                        {
                            ReportId = state.NextReportId, Title = title.Length > 200 ? title[..200] : title,
                            Level = severity, OccurredAtUtc = new DateTimeOffset(DateTime.SpecifyKind(state.StateSinceUtc, DateTimeKind.Utc)),
                            Content = $"State {state.CurrentState} since {state.StateSinceUtc:O}.\n\nServer: {settings.PublicBaseUrl.TrimEnd('/')}/Dashboard/Details/{Uri.EscapeDataString(state.ClientId)}",
                            Context = new JObject { ["source"] = Source, ["clientId"] = state.ClientId,
                                ["hostname"] = state.Hostname, ["state"] = state.CurrentState }
                        });
                        // Persist a fresh receipt for the next incident if a human closes this one.
                        state.NextReportId = Guid.NewGuid();
                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        foreach (var incident in owned.Where(item => string.CompareOrdinal(severity, item.Level) < 0))
                            await client.ChangeSeverityAsync(incident.IncidentId, new ChangeIncidentSeverityRequest
                            {
                                Version = incident.Version, Level = severity,
                                Reason = $"StatHub state {state.CurrentState} since {state.StateSinceUtc:O} now meets {severity}."
                            });
                    }
                }
                catch (Exception ex) { logger.LogError(ex, "Could not synchronize monitoring for agent {ClientId}", state.ClientId); }
            }
        }
        catch (Exception ex) { logger.LogError(ex, "AlertCenter monitoring synchronization failed; retained state for retry"); }
    }

    private async Task<List<IncidentDetails>> LoadActiveAsync()
    {
        var results = new List<IncidentDetails>();
        int? expectedTotal = null;
        for (var page = 1; ; page++)
        {
            var result = await client.QueryAsync(new QueryIncidentsRequest { ActiveOnly = true, Page = page, PageSize = 100 });
            expectedTotal ??= result.TotalCount;
            if (expectedTotal != result.TotalCount || result.Page != page || result.PageSize != 100)
                throw new InvalidOperationException("The active incident listing changed; synchronization will retry.");
            results.AddRange(result.Items);
            if (results.Count >= expectedTotal)
            {
                if (results.Count != expectedTotal || results.Select(item => item.IncidentId).Distinct().Count() != results.Count)
                    throw new InvalidOperationException("The active incident pages overlap or have inconsistent totals.");
                return results;
            }
            if (result.Items.Count == 0) throw new InvalidOperationException("The active incident listing is incomplete.");
        }
    }
}
