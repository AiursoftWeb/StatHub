using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.StatHub.Configuration;
using Aiursoft.StatHub.Entities.Monitoring;
using Aiursoft.StatHub.Services.Monitoring;
using Aiursoft.StatHub.Sqlite;
using Microsoft.EntityFrameworkCore;
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

    private MonitoringService Service() => new(_db, _coordinator, _settings, _client, _clock, NullLogger<MonitoringService>.Instance);

    [TestCleanup]
    public async Task Cleanup() { await _db.DisposeAsync(); _coordinator.Dispose(); }

    private Task Observe(params string[] colors) => _service.ObserveAsync("server-1", "Test server", colors);

    private async Task Continue(string color, int minutes)
    {
        for (var i = 0; i < minutes * 2; i++)
        {
            _clock.Advance(TimeSpan.FromSeconds(30));
            await Observe(color);
        }
    }

    [TestMethod]
    public async Task DefaultRulesSeedOnceAndDeletedRulesStayDeleted()
    {
        var policy = await _service.PolicyAsync();
        Assert.IsTrue(policy.IsEnabled && policy.MonitorAllServers && policy.AutoResolveOnGreen);
        Assert.AreEqual(5, policy.Rules.Count);
        CollectionAssert.AreEquivalent(new[] { "yellow:35:sev3", "yellow:100:sev2", "red:35:sev2", "red:100:sev1", "grey:15:sev2" },
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
        incident.Status = "resolved";
        await Continue(color, 1);
        Assert.AreEqual(2, _client.Incidents.Count);
        Assert.AreEqual(second, _client.Incidents.Last().Level);
    }

    [TestMethod]
    public async Task AnyGreenInBatchResolvesAllOwnedIncidentsAndResetsTimer()
    {
        await Observe("yellow");
        await Continue("yellow", 35);
        var duplicate = _client.Copy(_client.Incidents.Single());
        var foreign = _client.Copy(duplicate);
        foreign.Context!["source"] = "Different integration";
        await Observe("yellow", "green", "red");
        Assert.AreEqual(2, _client.Resolutions);
        Assert.AreEqual("new", foreign.Status);
        Assert.AreEqual("red", (await _db.AgentMonitoringStates.SingleAsync()).CurrentState);
        await Continue("red", 34);
        Assert.AreEqual(3, _client.Incidents.Count);
        await Continue("red", 1);
        Assert.AreEqual(4, _client.Incidents.Count);
        Assert.AreEqual("sev2", _client.Incidents.Last().Level);
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
        Assert.AreEqual(0, _client.Resolutions);
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual("sev2", _client.Incidents.Single().Level);
    }

    [TestMethod]
    public async Task RecoveryFailureSurvivesNextColorAndIsRetried()
    {
        await Observe("red");
        await Continue("red", 35);
        _client.FailResolution = true;
        await Observe("green", "yellow");
        Assert.IsNotNull((await _db.AgentMonitoringStates.SingleAsync()).PendingRecoveryAtUtc);
        _client.FailResolution = false;
        await _service.CheckAsync();
        Assert.AreEqual("resolved", _client.Incidents.Single().Status);
        Assert.IsNull((await _db.AgentMonitoringStates.SingleAsync()).PendingRecoveryAtUtc);
    }

    [TestMethod]
    public async Task LostCreateResponseDoesNotDuplicateAndAllowsRecreation()
    {
        _client.LoseCreateResponse = true;
        await Observe("yellow");
        await Continue("yellow", 36);
        Assert.AreEqual(1, _client.Incidents.Count);
        _client.Incidents.Single().Status = "resolved";
        await Observe("yellow");
        Assert.AreEqual(2, _client.Incidents.Count);
        Assert.AreEqual(1, _client.Incidents.Count(item => item.Status != "resolved"));
    }

    [TestMethod]
    public async Task RestartPreservesOfflineDeadlineButDoesNotInventContinuousRed()
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
        Assert.AreEqual("sev2", _client.Incidents.Single().Level);
        Assert.AreEqual("grey", (await _db.AgentMonitoringStates.SingleAsync()).CurrentState);
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
        Assert.AreEqual(0, _client.Resolutions);
        Assert.IsFalse(MonitoringService.Includes(policy, "server-1"));
        Assert.IsTrue(MonitoringService.Includes(policy, "new-server"));
    }

    [TestMethod]
    public async Task MissingValidSamplesNeverResetOfflineDeadlineOrResolveIncidents()
    {
        await Observe("red");
        var lastReport = (await _db.AgentMonitoringStates.SingleAsync()).LastReportedAtUtc;
        _clock.Advance(TimeSpan.FromMinutes(16));
        await Observe("unknown");
        Assert.AreEqual(lastReport, (await _db.AgentMonitoringStates.SingleAsync()).LastReportedAtUtc);
        await _service.CheckAsync();
        Assert.AreEqual("grey", (await _db.AgentMonitoringStates.SingleAsync()).CurrentState);
        Assert.AreEqual(1, _client.Incidents.Count);
        Assert.AreEqual(0, _client.Resolutions);
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
        Assert.AreEqual("red", (await _db.AgentMonitoringStates.SingleAsync()).CurrentState);
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
        public int Queries { get; private set; }
        public int Escalations { get; private set; }
        public int Resolutions { get; private set; }
        public bool FailResolution { get; set; }
        public bool LoseCreateResponse { get; set; }
        public Task<QueryIncidentsResponse> QueryAsync(QueryIncidentsRequest request)
        {
            Queries++;
            var active = Incidents.Where(item => item.Status != "resolved").ToList();
            return Task.FromResult(new QueryIncidentsResponse
            {
                Page = request.Page, PageSize = request.PageSize, TotalCount = active.Count,
                Items = active.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList()
            });
        }
        public Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request)
        {
            var incident = Incidents.SingleOrDefault(item => item.ReportId == request.ReportId);
            if (incident == null)
            {
                incident = new IncidentDetails { IncidentId = Guid.NewGuid(), Version = Guid.NewGuid(),
                    ReportId = request.ReportId, Title = request.Title, Level = request.Level, Status = "new",
                    Context = request.Context, ReceivedAtUtc = clock.GetUtcNow() };
                Incidents.Add(incident);
            }
            if (LoseCreateResponse) { LoseCreateResponse = false; throw new HttpRequestException("Lost response"); }
            return Task.FromResult(new CreateIncidentResponse { IncidentId = incident.IncidentId });
        }
        public Task<ResolveIncidentResponse> ResolveAsync(Guid incidentId, ResolveIncidentRequest request)
        {
            if (FailResolution) throw new HttpRequestException("Offline");
            var incident = Incidents.Single(item => item.IncidentId == incidentId);
            Assert.AreEqual(incident.Version, request.Version);
            incident.Status = "resolved";
            Resolutions++;
            return Task.FromResult(new ResolveIncidentResponse { IncidentId = incidentId });
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
                Level = original.Level, Status = "new", ReceivedAtUtc = original.ReceivedAtUtc, Context = (JObject)original.Context!.DeepClone() };
            Incidents.Add(copy);
            return copy;
        }
    }
}
