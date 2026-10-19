using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.StatHub.Configuration;
using Aiursoft.StatHub.Data;
using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.Entities.Monitoring;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Aiursoft.StatHub.Services.Monitoring;

public sealed class MonitoringService(StatHubDbContext db, MonitoringCoordinator coordinator,
    IAlertCenterSettingsProvider settingsProvider, IMonitoringIncidentClient client,
    TimeProvider clock, ILogger<MonitoringService> logger, InMemoryDatabase agents)
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

    private static DateTime? Since(AgentMonitoringState state, string threshold) => threshold switch
    {
        "yellow" => state.YellowOrWorseSinceUtc,
        "red" => state.RedOrWorseSinceUtc,
        "grey" when state.CurrentState == "grey" => state.StateSinceUtc,
        _ => null
    };

    private static MonitoringRule? MatchingRule(MonitoringPolicy policy, AgentMonitoringState state, DateTime now) =>
        policy.Rules.Where(rule => rule.IsEnabled && Since(state, rule.State) is { } since &&
                now - since >= TimeSpan.FromMinutes(rule.DurationMinutes))
            .OrderBy(rule => rule.Severity).ThenBy(rule => rule.DurationMinutes).ThenBy(rule => rule.Id).FirstOrDefault();

    public static string? Severity(MonitoringPolicy policy, AgentMonitoringState state, DateTime now) =>
        MatchingRule(policy, state, now)?.Severity;

    private static void Transition(AgentMonitoringState state, string color, DateTime at)
    {
        state.YellowOrWorseSinceUtc = color is "yellow" or "red" or "grey" ? state.YellowOrWorseSinceUtc ?? at : null;
        state.RedOrWorseSinceUtc = color is "red" or "grey" ? state.RedOrWorseSinceUtc ?? at : null;
        if (state.CurrentState != color) state.StateSinceUtc = at;
        state.CurrentState = color;
    }

    private void ResetUnobservedContinuity(AgentMonitoringState state)
    {
        // A process restart cannot prove live colors stayed unhealthy while observations were lost.
        // Persisted offline continuity is known from the last report and remains valid.
        if (state.ObservationSessionId == coordinator.SessionId || state.CurrentState == "grey") return;
        state.YellowOrWorseSinceUtc = null;
        state.RedOrWorseSinceUtc = null;
        state.CurrentState = "unknown";
    }

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
            ResetUnobservedContinuity(state);
            if (state.LastReportedAtUtc != default && now >= state.LastReportedAtUtc + OfflineTimeout)
                Transition(state, "grey", state.LastReportedAtUtc + OfflineTimeout);
            foreach (var color in colors)
            {
                Transition(state, color, now);
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
                    ResetUnobservedContinuity(state);
                    Transition(state, "grey", offlineAt);
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
                    var matchedRule = MatchingRule(policy, state, now);
                    var severity = matchedRule?.Severity;
                    if (severity == null || (state.CurrentState != "grey" && state.ObservationSessionId != coordinator.SessionId)) continue;
                    if (owned.Count == 0)
                    {
                        var title = $"{state.Hostname}: {state.CurrentState} server health";
                        await client.CreateAsync(new CreateIncidentRequest
                        {
                            ReportId = state.NextReportId, Title = title.Length > 200 ? title[..200] : title,
                            Level = severity, OccurredAtUtc = new DateTimeOffset(DateTime.SpecifyKind(Since(state, matchedRule!.State)!.Value, DateTimeKind.Utc)),
                            Content = Describe(policy, state, severity, now, settings),
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
                                Reason = $"StatHub has remained {matchedRule!.State} or worse since {Since(state, matchedRule.State):O}; the {matchedRule.DurationMinutes}-minute rule now meets {severity}. Current state: {state.CurrentState}."
                            });
                    }
                }
                catch (Exception ex) { logger.LogError(ex, "Could not synchronize monitoring for agent {ClientId}", state.ClientId); }
            }
        }
        catch (Exception ex) { logger.LogError(ex, "AlertCenter monitoring synchronization failed; retained state for retry"); }
    }

    private string Describe(MonitoringPolicy policy, AgentMonitoringState state, string severity, DateTime now, AlertCenterSettings settings)
    {
        var rule = MatchingRule(policy, state, now)!;
        var since = Since(state, rule.State)!.Value;
        var elapsed = (now - since).TotalMinutes;
        var summary = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Server {state.Hostname} has remained {rule.State} or worse for {elapsed:F1} minutes. " +
            $"The configured rule triggers {severity} after {rule.DurationMinutes} continuous minutes. Current state: {state.CurrentState}.\n" +
            $"Threshold exceeded since (UTC): {since:O}\nCurrent color since (UTC): {state.StateSinceUtc:O}\nLast report (UTC): {state.LastReportedAtUtc:O}\n");
        string evidence;
        string action;
        if (state.CurrentState == "grey")
        {
            evidence = "No agent reports have arrived for at least one minute, so StatHub marked the server offline. " +
                "This may indicate an agent, network or host problem; it does not by itself confirm a business outage.";
            action = "Check host reachability, the StatHub agent and its connection to StatHub. Then verify the services on this host.";
        }
        else
        {
            var agent = agents.GetClient(state.ClientId);
            evidence = "Live metric details are unavailable. Open the server page to inspect the latest observations.";
            if (agent != null && now - agent.LastUpdate <= OfflineTimeout && agent.CpuCores > 0)
            {
                var health = agent.GetHealthReport();
                var load = agent.GetLoad();
                evidence = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Latest available sample (UTC): {agent.LastUpdate:O}\n" +
                    $"CPU busy: {health.CpuRate}%; 15-minute load: {load.Load15M:F2} across {agent.CpuCores} logical CPUs " +
                    $"({health.LoadRate:F1}% per CPU); busiest disk: {health.DiskUseRatio:P1}.\n") +
                    "Red thresholds: CPU busy > 40%, load per CPU > 100%, or disk usage > 92%. " +
                    "Yellow thresholds: CPU busy > 20%, load per CPU > 70%, or disk usage > 80%, unless already red. " +
                    "These are the latest values, not a claim that one metric stayed above its threshold for the entire interval.";
            }
            action = "Inspect CPU-heavy processes and running builds, disk space and I/O, then check affected service responses. " +
                "A resource-pressure alert alone does not prove a service outage.";
        }
        var recovery = policy.AutoResolveOnGreen
            ? "One healthy (green) sample automatically resolves this server's active StatHub incidents."
            : "Automatic resolution on green is disabled. Verify recovery and resolve the incident manually.";
        var footer = $"\n\nNext steps: {action}\nRecovery: {recovery}\n" +
            $"Server details: {settings.PublicBaseUrl.TrimEnd('/')}/Dashboard/Details/{Uri.EscapeDataString(state.ClientId)}";
        var content = summary + "\nObserved evidence: " + evidence;
        return content[..Math.Min(content.Length, Math.Max(0, 4000 - footer.Length))] + footer;
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
