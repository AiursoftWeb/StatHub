using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.StatHub.Configuration;
using Aiursoft.StatHub.Data;
using Aiursoft.StatHub.SDK.Models;
using Aiursoft.StatHub.Entities.Monitoring;
using Aiursoft.StatHub.Services.Monitoring;
using Aiursoft.StatHub.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;

namespace Aiursoft.StatHub.Tests;

[TestClass]
public sealed class MonitoringTests
{
    private SqliteContext _db = null!;
    private readonly TestClock _clock = new();
    private MonitoringCoordinator _coordinator = null!;
    private RecordingClient _client = null!;
    private MonitoringService _service = null!;
    private readonly SettingsProvider _settings = new();
    private readonly InMemoryDatabase _agents = new();

    [TestInitialize]
    public async Task Initialize()
    {
        _db = new SqliteContext(new DbContextOptionsBuilder<SqliteContext>().UseSqlite("DataSource=:memory:").Options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.MigrateAsync();
        _coordinator = new MonitoringCoordinator();
        _client = new RecordingClient(_clock);
        _service = Service();
    }

    private MonitoringService Service() => new(_db, _coordinator, _settings, _client, _clock, NullLogger<MonitoringService>.Instance, _agents);

    [TestCleanup]
    public async Task Cleanup() { await _db.DisposeAsync(); _coordinator.Dispose(); }

    private async Task Observe(params string[] colors)
    {
        await _service.ObserveAsync("server-1", "Test server", colors);
        await _service.CheckAsync();
    }

    private async Task Continue(string color, int minutes)
    {
        for (var i = 0; i < minutes * 2; i++)
        {
            _clock.Advance(TimeSpan.FromSeconds(30));
            await Observe(color);
        }
    }

    [TestMethod]
    [DataRow("mitigated", 14399, true)]
    [DataRow("mitigated", 14400, true)]
    [DataRow("mitigated", 14401, false)]
    [DataRow("resolved", 14399, true)]
    [DataRow("resolved", 14400, true)]
    [DataRow("resolved", 14401, false)]
    public async Task RecurrenceReopensWithinFourHoursAndCreatesAfterBoundary(string status, int ageSeconds, bool reopen)
    {
        await Observe("yellow");
        await Continue("yellow", 35);
        var previous = _client.Incidents.Single();
        await Observe("green");
        previous.Status = status;
        if (status == "resolved") previous.ResolvedAtUtc = _clock.GetUtcNow();
        var oldVersion = previous.Version;
        var oldMitigated = previous.MitigatedAtUtc;
        var oldResolved = previous.ResolvedAtUtc;
        _clock.Advance(TimeSpan.FromSeconds(ageSeconds - 35 * 60));
        await Observe("green");
        // Recovery timestamps on completed incidents must never be refreshed by subsequent green samples.
        Assert.AreEqual(oldMitigated, previous.MitigatedAtUtc);
        Assert.AreEqual(oldResolved, previous.ResolvedAtUtc);
        await Observe("yellow");
        await Continue("yellow", 34);
        Assert.AreEqual(0, _client.Reopens);
        await Continue("yellow", 1);
        Assert.AreEqual(reopen ? 1 : 2, _client.Incidents.Count);
        Assert.AreEqual(reopen ? 1 : 0, _client.Reopens);
        var current = _client.Incidents.Single(item => item.Status == "new");
        Assert.AreEqual(reopen, current.IncidentId == previous.IncidentId);
        Assert.AreEqual("sev3", current.Level);
        if (!reopen)
        {
            Assert.AreEqual(oldVersion, previous.Version);
            Assert.AreEqual(oldMitigated, previous.MitigatedAtUtc);
            Assert.AreEqual(oldResolved, previous.ResolvedAtUtc);
        }
        await _service.CheckAsync();
        Assert.AreEqual(reopen ? 1 : 2, _client.Incidents.Count);
        await Continue("yellow", 65);
        Assert.AreEqual("sev2", current.Level);
        Assert.AreEqual(1, _client.Escalations);
        await Observe("green");
        Assert.AreEqual("mitigated", current.Status);
        Assert.AreEqual(2, _client.Mitigations);
        if (!reopen) Assert.AreEqual(oldVersion, previous.Version);
    }

    [TestMethod]
    public async Task MostRecentlyRecoveredIncidentIsReopenedEvenWhenAnOlderIncidentWasReceivedLater()
    {
        await Observe("yellow");
        await Continue("yellow", 35);
        var latest = _client.Incidents.Single();
        var older = _client.Copy(latest);
        older.Status = "resolved";
        older.MitigatedAtUtc = _clock.GetUtcNow().AddHours(-2);
        older.ResolvedAtUtc = _clock.GetUtcNow().AddHours(-1);
        older.ReceivedAtUtc = _clock.GetUtcNow().AddMinutes(1);
        await Observe("green");
        await Observe("yellow");
        await Continue("yellow", 35);
        Assert.AreEqual(1, _client.Reopens);
        Assert.AreEqual("new", latest.Status);
        Assert.AreEqual("resolved", older.Status);
        Assert.AreEqual(2, _client.Incidents.Count);
    }

    [TestMethod]
    public async Task RecentResolutionWinsOverEarlierMitigationAndSurvivesRestart()
    {
        await Observe("red");
        await Continue("red", 35);
        var previous = _client.Incidents.Single();
        await Observe("green");
        _clock.Advance(TimeSpan.FromHours(5));
        previous.Status = "resolved";
        previous.ResolvedAtUtc = _clock.GetUtcNow();
        _coordinator.Dispose();
        _coordinator = new MonitoringCoordinator();
        _service = Service();
        await Observe("red");
        await Continue("red", 35);
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual(1, _client.Reopens);
        Assert.AreEqual("new", previous.Status);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ReopenFailureOrLostResponseNeverCreatesDuplicate(bool lostResponse)
    {
        await Observe("yellow");
        await Continue("yellow", 35);
        await Observe("green");
        _client.FailReopen = !lostResponse;
        _client.LoseReopenResponse = lostResponse;
        await Observe("yellow");
        await Continue("yellow", 35);
        Assert.AreEqual(1, _client.Incidents.Count);
        _client.FailReopen = false;
        await _service.CheckAsync();
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual(1, _client.Reopens);
        Assert.AreEqual("new", _client.Incidents.Single().Status);
    }

    [TestMethod]
    public async Task DelayedRecoveryAcknowledgementReopensTheSameIncidentForAnAlreadyMatureRecurrence()
    {
        await Observe("red");
        await Continue("red", 35);
        _client.FailMitigation = true;
        await Observe("green", "red");
        await Continue("red", 35);
        _client.FailMitigation = false;
        await _service.CheckAsync();
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual(1, _client.Reopens);
        Assert.AreEqual(1, _client.Mitigations);
        Assert.AreEqual("new", _client.Incidents.Single().Status);
    }

    [TestMethod]
    public async Task GreenDuringReopenMitigatesAgainWithoutCreatingAnotherIncident()
    {
        await Observe("red");
        await Continue("red", 35);
        await Observe("green");
        await Observe("red");
        await Continue("red", 34);
        _client.BeforeReopen = async () =>
        {
            await _service.ObserveAsync("server-1", "Test server", ["green", "red"]);
        };
        await Continue("red", 1);
        Assert.AreEqual(1, _client.Reopens);
        await _service.CheckAsync();
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual("mitigated", _client.Incidents.Single().Status);
        Assert.AreEqual(2, _client.Mitigations);
    }

    [TestMethod]
    [DataRow("source", "Another integration")]
    [DataRow("clientId", "another-server")]
    public async Task ActiveIncidentTakesPrecedenceAndForeignHistoryIsNeverReopened(string property, string value)
    {
        await Observe("yellow");
        await Continue("yellow", 35);
        var active = _client.Incidents.Single();
        var closed = _client.Copy(active);
        closed.Status = "mitigated";
        closed.MitigatedAtUtc = _clock.GetUtcNow();
        await Continue("yellow", 65);
        Assert.AreEqual("sev2", active.Level);
        Assert.AreEqual("sev3", closed.Level);
        Assert.AreEqual(0, _client.Reopens);
        await Observe("green");
        foreach (var incident in _client.Incidents)
            incident.Context![property] = value;
        await Observe("yellow");
        await Continue("yellow", 35);
        Assert.AreEqual(3, _client.Incidents.Count);
        Assert.AreEqual(0, _client.Reopens);
    }

    [TestMethod]
    public async Task MigrationRemovesRuntimeStateAndPreservesRules()
    {
        var migrator = _db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261019063634_SeverityThresholdTimers");
        var since = _clock.GetUtcNow().UtcDateTime;
        var id = Guid.NewGuid();
        await _db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AgentMonitoringStates (ClientId, Hostname, CurrentState, LastReportedAtUtc, StateSinceUtc, ObservationSessionId, NextReportId) VALUES ('old', 'old', 'red', {since}, {since}, {id}, {id})");
        var policy = await _service.PolicyAsync();
        policy.Rules.Single(rule => rule.State == "yellow" && rule.DurationMinutes == 35).DurationMinutes = 42;
        await _db.SaveChangesAsync();
        await migrator.MigrateAsync();
        using var command = _db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AgentMonitoringStates'";
        Assert.AreEqual(0L, await command.ExecuteScalarAsync());
        Assert.AreEqual(6, await _db.AlertMonitoringRules.CountAsync());
        Assert.IsTrue(await _db.AlertMonitoringRules.AnyAsync(rule => rule.DurationMinutes == 42));
    }

    [TestMethod]
    public async Task AlternatingYellowAndRedTriggersYellowThresholdWithoutContinuousRed()
    {
        await Observe("yellow");
        for (var i = 0; i < 7; i++) await Continue(i % 2 == 0 ? "yellow" : "red", 5);
        Assert.AreEqual("sev3", _client.Incidents.Single().Level);
        Assert.Contains("yellow or worse", _client.Created.Single().Content!);
        for (var i = 0; i < 13; i++) await Continue(i % 2 == 0 ? "red" : "yellow", 5);
        Assert.AreEqual("sev2", _client.Incidents.Single().Level);
        Assert.AreEqual(1, _client.Escalations);
    }

    [TestMethod]
    public async Task RedThenOfflinePreservesRedTimerAndMatchesSeveralRulesWithoutDuplicates()
    {
        await Observe("red");
        await Continue("red", 20);
        _clock.Advance(TimeSpan.FromMinutes(15));
        await _service.CheckAsync();
        Assert.AreEqual("sev2", _client.Incidents.Single().Level);
        Assert.Contains("red or worse", _client.Created.Single().Content!);
        _clock.Advance(TimeSpan.FromMinutes(1));
        await _service.CheckAsync();
        Assert.AreEqual(1, _client.Incidents.Count);
        _clock.Advance(TimeSpan.FromMinutes(64));
        await _service.CheckAsync();
        Assert.AreEqual("sev1", _client.Incidents.Single().Level);
        Assert.AreEqual(1, _client.Escalations);
    }

    [TestMethod]
    public async Task ReconnectionWithoutOfflineSweepStillCountsGreyAndResetsOnlyRecoveredThresholds()
    {
        await Observe("red");
        await Continue("red", 20);
        var redStart = _coordinator.Snapshot().Single().RedOrWorseSinceUtc;
        _clock.Advance(TimeSpan.FromMinutes(10));
        await Observe("red");
        Assert.AreEqual(redStart, _coordinator.Snapshot().Single().RedOrWorseSinceUtc);
        await Continue("red", 5);
        Assert.AreEqual("sev2", _client.Incidents.Single().Level);
        await Observe("yellow");
        var state = _coordinator.Snapshot().Single();
        Assert.IsNull(state.RedOrWorseSinceUtc);
        Assert.AreEqual(redStart, state.YellowOrWorseSinceUtc);
        Assert.AreEqual("sev2", _client.Incidents.Single().Level);
    }

    [TestMethod]
    public async Task RestartDiscardsOldGreyAndBeginsFreshObservation()
    {
        await Observe("green");
        _clock.Advance(TimeSpan.FromMinutes(10));
        await _service.CheckAsync();
        var state = _coordinator.Snapshot().Single();
        var start = state.RedOrWorseSinceUtc;
        Assert.AreEqual(state.StateSinceUtc, start);
        _db.ChangeTracker.Clear();
        _coordinator.Dispose();
        _coordinator = new MonitoringCoordinator();
        _service = Service();
        await Observe("red");
        state = _coordinator.Snapshot().Single();
        Assert.AreNotEqual(start, state.RedOrWorseSinceUtc);
        Assert.AreEqual(_clock.GetUtcNow().UtcDateTime, state.RedOrWorseSinceUtc);
        Assert.AreEqual("red", state.CurrentState);
        await Continue("red", 34);
        Assert.IsEmpty(_client.Incidents);
        await Continue("red", 1);
        Assert.AreEqual("sev2", _client.Incidents.Single().Level);
        Assert.Contains("red or worse", _client.Created.Single().Content!);
    }

    [TestMethod]
    public async Task EveryLiveColorTransitionKeepsOnlyApplicableTimers()
    {
        foreach (var from in new[] { "green", "yellow", "red", "grey" })
        foreach (var to in new[] { "green", "yellow", "red" })
        {
            var id = $"{from}-{to}";
            await _service.ObserveAsync(id, id, [from == "grey" ? "green" : from]);
            if (from == "grey")
            {
                _clock.Advance(TimeSpan.FromMinutes(2));
                await _service.CheckAsync();
            }
            var before = _coordinator.Snapshot().Single(item => item.ClientId == id);
            var yellow = before.YellowOrWorseSinceUtc;
            var red = before.RedOrWorseSinceUtc;
            _clock.Advance(TimeSpan.FromSeconds(30));
            await _service.ObserveAsync(id, id, [to]);
            before = _coordinator.Snapshot().Single(item => item.ClientId == id);
            var now = _clock.GetUtcNow().UtcDateTime;
            Assert.AreEqual(to == "green" ? null : yellow ?? now, before.YellowOrWorseSinceUtc, id);
            Assert.AreEqual(to == "red" ? red ?? now : null, before.RedOrWorseSinceUtc, id);
        }
    }

    [TestMethod]
    public async Task MemoryTimersSurviveRequestScopesAndRespectCustomDisabledRules()
    {
        var policy = await _service.PolicyAsync();
        foreach (var rule in policy.Rules) rule.IsEnabled = false;
        _db.AlertMonitoringRules.Add(new MonitoringRule { Policy = policy, State = "yellow", DurationMinutes = 2, Severity = "sev4" });
        await _db.SaveChangesAsync();
        await Observe("red");
        await Continue("red", 1);
        _db.ChangeTracker.Clear();
        _service = Service();
        await Continue("yellow", 1);
        Assert.AreEqual("sev4", _client.Incidents.Single().Level);
        await Observe("green", "red");
        Assert.AreEqual("mitigated", _client.Incidents.Single().Status);
        await Continue("red", 1);
        Assert.AreEqual(1, _client.Incidents.Count);
        await Continue("red", 1);
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual(1, _client.Reopens);
        Assert.AreEqual("new", _client.Incidents.Single().Status);
    }

    [TestMethod]
    public async Task DescriptionIncludesRuleLiveEvidenceActionsAndRecovery()
    {
        var agent = _agents.GetOrAddClient("server-1");
        agent.CpuCores = 24;
        agent.TotalRoot = 100;
        agent.UsedRoot = 40;
        await agent.Stats.BroadcastAsync(new DstatResult { CpuIdl = 22, Load15M = 25.31 });
        await Observe("red");
        await Continue("red", 34);
        agent.LastUpdate = _clock.GetUtcNow().UtcDateTime.AddMinutes(1);
        await Continue("red", 1);
        var content = _client.Created.Single().Content!;
        Assert.Contains("35 continuous minutes", content);
        Assert.Contains("CPU busy: 78%", content);
        Assert.Contains("25.31 across 24", content);
        Assert.Contains("40.0", content);
        Assert.Contains("Next steps:", content);
        Assert.Contains("automatically mitigates", content);
        Assert.Contains("/Dashboard/Details/server-1", content);
        Assert.IsTrue(content.Length <= 4000);
    }

    [TestMethod]
    public async Task OfflineDescriptionDoesNotPresentOldMetricsAsLive()
    {
        var agent = _agents.GetOrAddClient("server-1");
        agent.CpuCores = 24;
        await Observe("red");
        _clock.Advance(TimeSpan.FromMinutes(16));
        await _service.CheckAsync();
        var content = _client.Created.Single().Content!;
        Assert.Contains("15 continuous minutes", content);
        Assert.Contains("No agent reports", content);
        Assert.Contains("Last report (UTC):", content);
        Assert.DoesNotContain("CPU busy:", content);
        Assert.Contains("host reachability", content);
    }

    [TestMethod]
    public async Task MissingMetricsAndDisabledRecoveryAreExplained()
    {
        var policy = await _service.PolicyAsync();
        policy.AutoResolveOnGreen = false;
        await _db.SaveChangesAsync();
        await Observe("yellow");
        await Continue("yellow", 35);
        var content = _client.Created.Single().Content!;
        Assert.Contains("Live metric details are unavailable", content);
        Assert.Contains("mitigate the incident manually", content);
    }

    [TestMethod]
    public async Task GreyCreatesAtFifteenMinutesAndEscalatesSameIncidentAtThirtyFive()
    {
        await Observe("green");
        _clock.Advance(MonitoringService.OfflineTimeout + TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));
        await _service.CheckAsync();
        Assert.IsEmpty(_client.Incidents);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _service.CheckAsync();
        var incident = _client.Incidents.Single();
        Assert.AreEqual("sev3", incident.Level);
        Assert.AreEqual(_clock.GetUtcNow().AddMinutes(-15), incident.OccurredAtUtc);
        Assert.Contains("15 continuous minutes", _client.Created.Single().Content!);
        _clock.Advance(TimeSpan.FromMinutes(20) - TimeSpan.FromSeconds(1));
        await _service.CheckAsync();
        Assert.AreEqual("sev3", incident.Level);
        Assert.AreEqual(0, _client.Escalations);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _service.CheckAsync();
        Assert.AreEqual("sev2", incident.Level);
        Assert.AreEqual(1, _client.Escalations);
        await _service.CheckAsync();
        Assert.AreSame(incident, _client.Incidents.Single());
        Assert.AreEqual(1, _client.Escalations);
        await Observe("green");
        Assert.AreEqual("mitigated", incident.Status);
    }

    [TestMethod]
    public async Task GreenBeforeGreyEscalationResetsTheGreyTimer()
    {
        await Observe("green");
        _clock.Advance(MonitoringService.OfflineTimeout + TimeSpan.FromMinutes(15));
        await _service.CheckAsync();
        var incident = _client.Incidents.Single();
        await Observe("green");
        _clock.Advance(MonitoringService.OfflineTimeout + TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));
        await _service.CheckAsync();
        Assert.AreEqual("mitigated", incident.Status);
        Assert.AreEqual(0, _client.Reopens);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _service.CheckAsync();
        Assert.AreEqual(1, _client.Reopens);
        Assert.AreEqual("sev3", incident.Level);
        Assert.AreEqual(0, _client.Escalations);
        Assert.AreEqual(1, _client.Incidents.Count);
    }

    [TestMethod]
    public async Task DefaultRulesSeedOnceAndDeletedRulesStayDeleted()
    {
        var policy = await _service.PolicyAsync();
        Assert.IsTrue(policy.IsEnabled && policy.MonitorAllServers && policy.AutoResolveOnGreen);
        Assert.AreEqual(6, policy.Rules.Count);
        CollectionAssert.AreEquivalent(new[] { "yellow:35:sev3", "yellow:100:sev2", "red:35:sev2", "red:100:sev1", "grey:15:sev3", "grey:35:sev2" },
            policy.Rules.Select(rule => $"{rule.State}:{rule.DurationMinutes}:{rule.Severity}").ToArray());
        Assert.IsEmpty(policy.Targets);
        _db.AlertMonitoringRules.RemoveRange(policy.Rules);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        Assert.IsEmpty((await Service().PolicyAsync()).Rules);
    }

    [TestMethod]
    [DataRow("yellow", "sev3", "sev2")]
    [DataRow("red", "sev2", "sev1")]
    public async Task SustainedRulesCreateEscalateDeduplicateAndRecreateAfterHumanClosure(string color, string first, string second)
    {
        await Observe(color);
        await Continue(color, 34);
        Assert.IsEmpty(_client.Incidents);
        await Continue(color, 1);
        var incident = _client.Incidents.Single();
        Assert.AreEqual(first, incident.Level);
        await Continue(color, 65);
        Assert.AreEqual(second, incident.Level);
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual(1, _client.Escalations);
        incident.Status = "mitigated";
        incident.MitigatedAtUtc = _clock.GetUtcNow();
        await Continue(color, 1);
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual(1, _client.Reopens);
        Assert.AreEqual("new", incident.Status);
        Assert.AreEqual(second, _client.Incidents.Last().Level);
    }

    [TestMethod]
    public async Task AnyGreenInBatchMitigatesAllOwnedIncidentsAndResetsTimer()
    {
        await Observe("yellow");
        await Continue("yellow", 35);
        var duplicate = _client.Copy(_client.Incidents.Single());
        var foreign = _client.Copy(duplicate);
        foreign.Context!["source"] = "Different integration";
        await Observe("yellow", "green", "red");
        Assert.AreEqual(2, _client.Mitigations);
        Assert.AreEqual("new", foreign.Status);
        Assert.AreEqual("red", _coordinator.Snapshot().Single().CurrentState);
        await Continue("red", 34);
        Assert.AreEqual(3, _client.Incidents.Count);
        await Continue("red", 1);
        Assert.AreEqual(3, _client.Incidents.Count);
        Assert.AreEqual(1, _client.Reopens);
        Assert.AreEqual(1, _client.Incidents.Count(item => item.Context!.Value<string>("source") == "StatHub.HealthMonitoring" && item.Status == "new"));
        Assert.AreEqual("sev2", _client.Incidents.First(item => item.Status == "new" && item.Context!.Value<string>("source") == "StatHub.HealthMonitoring").Level);
    }

    [TestMethod]
    public async Task DisabledGreenResolutionLeavesIncidentButStillResetsTimer()
    {
        var policy = await _service.PolicyAsync();
        policy.AutoResolveOnGreen = false;
        await _db.SaveChangesAsync();
        await Observe("red");
        await Continue("red", 35);
        await Observe("green", "red");
        await Continue("red", 34);
        Assert.AreEqual(0, _client.Mitigations);
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual("sev2", _client.Incidents.Single().Level);
    }

    [TestMethod]
    public async Task RecoveryFailureSurvivesNextColorAndIsRetried()
    {
        await Observe("red");
        await Continue("red", 35);
        _client.FailMitigation = true;
        await Observe("green", "yellow");
        Assert.IsNotNull(_coordinator.Snapshot().Single().PendingRecoveryAtUtc);
        _client.FailMitigation = false;
        await _service.CheckAsync();
        Assert.AreEqual("mitigated", _client.Incidents.Single().Status);
        Assert.IsNull(_coordinator.Snapshot().Single().PendingRecoveryAtUtc);
    }

    [TestMethod]
    [DataRow("mitigated")]
    [DataRow("resolved")]
    public async Task LostCreateResponseDoesNotDuplicateAndAllowsRecreation(string inactiveStatus)
    {
        _client.LoseCreateResponse = true;
        await Observe("yellow");
        await Continue("yellow", 36);
        Assert.AreEqual(1, _client.Incidents.Count);
        var incident = _client.Incidents.Single();
        incident.Status = inactiveStatus;
        incident.MitigatedAtUtc = _clock.GetUtcNow();
        if (inactiveStatus == "resolved") incident.ResolvedAtUtc = _clock.GetUtcNow();
        await Observe("yellow");
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual(1, _client.Reopens);
        Assert.AreEqual(1, _client.Incidents.Count(item => item.Status is not ("mitigated" or "resolved")));
    }

    [TestMethod]
    public async Task RestartStartsFreshTimersAndDetectsSubsequentOffline()
    {
        await Observe("red");
        await Continue("red", 34);
        _coordinator.Dispose();
        _coordinator = new MonitoringCoordinator();
        _service = Service();
        await Observe("red");
        await Continue("red", 1);
        Assert.IsEmpty(_client.Incidents);
        _clock.Advance(TimeSpan.FromMinutes(15));
        await _service.CheckAsync();
        Assert.IsEmpty(_client.Incidents);
        _clock.Advance(TimeSpan.FromMinutes(1));
        await _service.CheckAsync();
        Assert.AreEqual("sev3", _client.Incidents.Single().Level);
        Assert.AreEqual("grey", _coordinator.Snapshot().Single().CurrentState);
        await _service.CheckAsync();
        Assert.AreEqual(1, _client.Incidents.Count);
    }

    [TestMethod]
    public async Task SelectionExclusionAndImmediateRulesAreRespected()
    {
        var policy = await _service.PolicyAsync();
        _db.AlertMonitoringRules.Add(new MonitoringRule { Policy = policy, State = "yellow", DurationMinutes = 0, Severity = "sev4" });
        policy.MonitorAllServers = false;
        await _db.SaveChangesAsync();
        await Observe("yellow");
        Assert.IsEmpty(_client.Incidents);
        policy.Targets.Add(new MonitoringTarget { ClientId = "server-1" });
        await _db.SaveChangesAsync();
        await Observe("yellow");
        Assert.AreEqual("sev4", _client.Incidents.Single().Level);
        policy.MonitorAllServers = true;
        policy.Targets.Single().IsExcluded = true;
        await _db.SaveChangesAsync();
        await Observe("green");
        Assert.AreEqual(0, _client.Mitigations);
        await _service.ObserveAsync("new-server", "New server", ["yellow"]);
        await _service.CheckAsync();
        Assert.AreEqual(2, _client.Incidents.Count);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SelectionRefreshPreservesExclusionPrecedenceAndRecovery(bool monitorAll)
    {
        var policy = await _service.PolicyAsync();
        policy.MonitorAllServers = monitorAll;
        _db.AlertMonitoringRules.Add(new MonitoringRule { Policy = policy, State = "yellow", DurationMinutes = 0, Severity = "sev4" });
        policy.Targets.Add(new MonitoringTarget { ClientId = "selected" });
        policy.Targets.Add(new MonitoringTarget { ClientId = "excluded", IsExcluded = true });
        await _db.SaveChangesAsync();
        foreach (var id in new[] { "selected", "excluded", "unlisted" })
            await _service.ObserveAsync(id, id, ["yellow"]);
        await _service.CheckAsync();
        Assert.AreEqual(monitorAll ? 2 : 1, _client.Incidents.Count);
        Assert.IsFalse(_client.Incidents.Any(incident => incident.Context!.Value<string>("clientId") == "excluded"));

        // The next pass must see an edited selection, including recovery eligibility.
        policy.Targets.Single(target => target.ClientId == "selected").IsExcluded = true;
        policy.Targets.Single(target => target.ClientId == "excluded").IsExcluded = false;
        await _db.SaveChangesAsync();
        await _service.ObserveAsync("selected", "selected", ["green"]);
        await _service.CheckAsync();
        Assert.AreEqual(0, _client.Mitigations);
        Assert.AreEqual(monitorAll ? 3 : 2, _client.Incidents.Count);
        Assert.IsFalse(await _service.ObserveAsync("selected", "selected", ["red", "green"]));
        Assert.IsTrue(await _service.ObserveAsync("excluded", "excluded", ["green"]));
        await _service.CheckAsync();
        Assert.AreEqual(1, _client.Mitigations);

        policy.Targets.Single(target => target.ClientId == "selected").IsExcluded = false;
        await _db.SaveChangesAsync();
        await _service.ObserveAsync("selected", "selected", ["green"]);
        await _service.CheckAsync();
        Assert.AreEqual(2, _client.Mitigations);
    }

    [TestMethod]
    public async Task MissingValidSamplesNeverResetOfflineDeadlineOrMitigateIncidents()
    {
        await Observe("red");
        var lastReport = _coordinator.Snapshot().Single().LastReportedAtUtc;
        _clock.Advance(TimeSpan.FromMinutes(16));
        await Observe("unknown");
        Assert.AreEqual(lastReport, _coordinator.Snapshot().Single().LastReportedAtUtc);
        await _service.CheckAsync();
        Assert.AreEqual("grey", _coordinator.Snapshot().Single().CurrentState);
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual(0, _client.Mitigations);
    }

    [TestMethod]
    public async Task DisabledIntegrationObservesWithoutNetworkCalls()
    {
        _settings.Value.Enabled = false;
        await Observe("green", "red");
        await Continue("red", 36);
        await _service.CheckAsync();
        Assert.AreEqual(0, _client.Queries);
        Assert.IsEmpty(_client.Incidents);
        Assert.AreEqual("red", _coordinator.Snapshot().Single().CurrentState);
    }

    [TestMethod]
    public async Task ReportsNeedNoDatabaseOrNetworkAndKeepOnlyOneStatePerServer()
    {
        // Closing the SQLite in-memory database makes any accidental SQL access fail.
        await _db.Database.CloseConnectionAsync();
        var start = _clock.GetUtcNow().UtcDateTime;
        for (var sample = 0; sample < 1000; sample++)
        {
            for (var server = 0; server < 100; server++)
                await _service.ObserveAsync($"server-{server}", "Server", [sample % 2 == 0 ? "yellow" : "red"]);
            _clock.Advance(TimeSpan.FromSeconds(1));
        }
        Assert.AreEqual(100, _coordinator.Snapshot().Count);
        Assert.IsTrue(_coordinator.Snapshot().All(state => state.YellowOrWorseSinceUtc == start));
        Assert.AreEqual(0, _client.Queries);
        Assert.IsEmpty(_client.Created);
    }

    [TestMethod]
    public async Task OneSyncQueriesOnceForAllServersAndWarmHealthyReportsDoNotWakeJobs()
    {
        for (var server = 0; server < 100; server++)
            Assert.IsFalse(await _service.ObserveAsync($"server-{server}", "Server", ["green"]));
        await _service.CheckAsync();
        Assert.AreEqual(1, _client.Queries);
        for (var server = 0; server < 100; server++)
            Assert.IsFalse(await _service.ObserveAsync($"server-{server}", "Server", ["green"]));
        Assert.AreEqual(1, _client.Queries);
    }

    [TestMethod]
    public async Task RecoveryWakeIsCoalescedAndTransientGreenSurvivesSlowQuery()
    {
        await Observe("red");
        await Continue("red", 35);
        Assert.IsTrue(await _service.ObserveAsync("server-1", "Server", ["green", "red"]));
        Assert.IsFalse(await _service.ObserveAsync("server-1", "Server", ["green", "red"]));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.BeforeQuery = async () => { started.SetResult(); await release.Task; };
        var sync = _service.CheckAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _service.ObserveAsync("server-1", "Server", ["green", "red"]).WaitAsync(TimeSpan.FromSeconds(1));
        release.SetResult();
        await sync;
        Assert.AreEqual("mitigated", _client.Incidents.Single().Status);
        Assert.AreEqual("red", _coordinator.Snapshot().Single().CurrentState);
        Assert.IsNull(_coordinator.Snapshot().Single().PendingRecoveryAtUtc);
    }

    [TestMethod]
    public async Task GreenDuringCreateMitigatesTheIncidentEvenIfItsResponseArrivesLater()
    {
        await Observe("red");
        await Continue("red", 34);
        _client.BeforeCreate = async () =>
        {
            _clock.Advance(TimeSpan.FromSeconds(1));
            await _service.ObserveAsync("server-1", "Server", ["green", "red"]);
            _clock.Advance(TimeSpan.FromSeconds(1));
        };
        await Continue("red", 1);
        Assert.AreEqual(1, _client.Incidents.Count);
        await _service.CheckAsync();
        Assert.AreEqual("mitigated", _client.Incidents.Single().Status);
        Assert.AreEqual(1, _client.Mitigations);
    }

    [TestMethod]
    public async Task RestartReconcilesExistingIncidentsWithoutRecreatingOrMitigatingBeforeObservation()
    {
        await Observe("red");
        await Continue("red", 35);
        _coordinator.Dispose();
        _coordinator = new MonitoringCoordinator();
        _service = Service();
        await _service.CheckAsync();
        Assert.AreEqual("new", _client.Incidents.Single().Status);
        Assert.IsEmpty(_coordinator.Snapshot());
        await Observe("red");
        await Continue("red", 35);
        Assert.AreEqual(1, _client.Incidents.Count);
        await Observe("green");
        Assert.AreEqual("mitigated", _client.Incidents.Single().Status);
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }

    private sealed class SettingsProvider : IAlertCenterSettingsProvider
    {
        public AlertCenterSettings Value { get; } = new() { Enabled = true, ApiKey = "test-only" };
        public Task<AlertCenterSettings> GetAsync() => Task.FromResult(Value);
    }

    private sealed class RecordingClient(TestClock clock) : IMonitoringIncidentClient
    {
        public List<IncidentDetails> Incidents { get; } = [];
        public List<CreateIncidentRequest> Created { get; } = [];
        public int Queries { get; private set; }
        public int Escalations { get; private set; }
        public int Mitigations { get; private set; }
        public int Reopens { get; private set; }
        public bool LoseReopenResponse { get; set; }
        public bool FailReopen { get; set; }
        public Func<Task>? BeforeReopen { get; set; }
        public Func<Task>? BeforeQuery { get; set; }
        public Func<Task>? BeforeCreate { get; set; }
        public bool FailMitigation { get; set; }
        public bool LoseCreateResponse { get; set; }
        public async Task<QueryIncidentsResponse> QueryAsync(QueryIncidentsRequest request)
        {
            Queries++;
            if (BeforeQuery != null) await BeforeQuery();
            var active = Incidents.Where(item => !request.ActiveOnly || item.Status != "resolved")
                .Where(item => !request.RecoveredSinceUtc.HasValue || item.Status is not ("mitigated" or "resolved") ||
                    item.MitigatedAtUtc >= request.RecoveredSinceUtc || item.ResolvedAtUtc >= request.RecoveredSinceUtc).ToList();
            return new QueryIncidentsResponse
            {
                Page = request.Page, PageSize = request.PageSize, TotalCount = active.Count,
                Items = active.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
                    .Select(item => JObject.FromObject(item).ToObject<IncidentDetails>()!).ToList()
            };
        }
        public async Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request)
        {
            if (BeforeCreate != null) await BeforeCreate();
            Created.Add(request);
            var incident = Incidents.SingleOrDefault(item => item.ReportId == request.ReportId);
            if (incident == null)
            {
                incident = new IncidentDetails { IncidentId = Guid.NewGuid(), Version = Guid.NewGuid(),
                    ReportId = request.ReportId, Title = request.Title, Level = request.Level, Status = "new",
                    Context = request.Context, ReceivedAtUtc = clock.GetUtcNow(), OccurredAtUtc = request.OccurredAtUtc };
                Incidents.Add(incident);
            }
            if (LoseCreateResponse) { LoseCreateResponse = false; throw new HttpRequestException("Lost response"); }
            return new CreateIncidentResponse { IncidentId = incident.IncidentId };
        }
        public Task<MitigateIncidentResponse> MitigateAsync(Guid incidentId, MitigateIncidentRequest request)
        {
            if (FailMitigation) throw new HttpRequestException("Offline");
            var incident = Incidents.Single(item => item.IncidentId == incidentId);
            Assert.AreEqual(incident.Version, request.Version);
            if (incident.Status == "mitigated")
                return Task.FromResult(new MitigateIncidentResponse { IncidentId = incidentId, Version = incident.Version, MitigatedAtUtc = incident.MitigatedAtUtc });
            incident.Status = "mitigated";
            incident.MitigatedAtUtc = clock.GetUtcNow();
            incident.Version = Guid.NewGuid();
            Mitigations++;
            return Task.FromResult(new MitigateIncidentResponse { IncidentId = incidentId, Version = incident.Version, MitigatedAtUtc = incident.MitigatedAtUtc });
        }
        public async Task<ReopenIncidentResponse> ReopenAsync(Guid incidentId, ReopenIncidentRequest request)
        {
            if (BeforeReopen != null) await BeforeReopen();
            if (FailReopen) throw new HttpRequestException("Reopen unavailable");
            var incident = Incidents.Single(item => item.IncidentId == incidentId);
            Assert.AreEqual(incident.Version, request.Version);
            incident.Status = "new";
            incident.MitigatedAtUtc = null;
            incident.ResolvedAtUtc = null;
            incident.Version = Guid.NewGuid();
            Reopens++;
            if (LoseReopenResponse) { LoseReopenResponse = false; throw new HttpRequestException("Lost reopen response"); }
            return new ReopenIncidentResponse { IncidentId = incidentId, Version = incident.Version };
        }
        public Task<ChangeIncidentSeverityResponse> ChangeSeverityAsync(Guid incidentId, ChangeIncidentSeverityRequest request)
        {
            var incident = Incidents.Single(item => item.IncidentId == incidentId);
            Assert.AreEqual(incident.Version, request.Version);
            incident.Level = request.Level;
            incident.Version = Guid.NewGuid();
            Escalations++;
            return Task.FromResult(new ChangeIncidentSeverityResponse { IncidentId = incidentId, Level = request.Level });
        }
        public IncidentDetails Copy(IncidentDetails original)
        {
            var copy = new IncidentDetails { IncidentId = Guid.NewGuid(), Version = Guid.NewGuid(), ReportId = Guid.NewGuid(),
                Level = original.Level, Status = "new", OccurredAtUtc = original.OccurredAtUtc, ReceivedAtUtc = original.ReceivedAtUtc, Context = (JObject)original.Context!.DeepClone() };
            Incidents.Add(copy);
            return copy;
        }
    }
}
