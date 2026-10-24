using System.Globalization;
using Aiursoft.StatHub.Client.Services;
using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Tests.UnitTests;

[TestClass]
public sealed class ResourceHealthTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTime Now = new(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private readonly Clock _clock = new();
    private Agent _agent = null!;

    [TestInitialize]
    public void Initialize() => _agent = new Agent("server", _clock) { CpuCores = 24, TotalRoot = 100, UsedRoot = 40 };

    private async Task Sample(int idle = 50, double cpu = 0, double memory = 0, double io = 0, DateTime? at = null)
    {
        _agent.LastUpdate = _clock.Now;
        await _agent.Stats.BroadcastAsync(new DstatResult
        {
            SampleTime = at ?? _clock.Now, SampleId = Guid.NewGuid(), CpuIdl = idle,
            Pressure = new PressureInfo(cpu, memory, io), Load15M = 100
        });
    }

    private async Task Run(int seconds, int idle = 50, double cpu = 0, double memory = 0, double io = 0)
    {
        await Sample(idle, cpu, memory, io);
        for (var i = 0; i < seconds; i += 10)
        {
            _clock.Now = _clock.Now.AddSeconds(10);
            await Sample(idle, cpu, memory, io);
        }
    }

    private AgentStatus Status => _agent.GetHealthReport().Status;

    [TestMethod]
    public async Task BoundedCompilationDoesNotTurnRedFromBusyCpuOrLargeLoadAlone()
    {
        await Run(290, idle: 40, cpu: 90);
        Assert.AreEqual(AgentStatus.Unknown, Status);
        await Run(10, idle: 40, cpu: 90);
        Assert.AreEqual(AgentStatus.Healthy, Status);
        await Run(3600, idle: 40, cpu: 90);
        Assert.AreEqual(AgentStatus.Healthy, Status);
        Assert.AreEqual(100.0 / 24, _agent.GetHealthReport().LoadRate / 100, 0.0001);
    }

    [TestMethod]
    [DataRow(15, 20.0, 0.0, 0.0, 40.0, AgentStatus.Warning)]
    [DataRow(5, 50.0, 0.0, 0.0, 40.0, AgentStatus.Critical)]
    [DataRow(50, 0.0, 5.0, 0.0, 40.0, AgentStatus.Warning)]
    [DataRow(50, 0.0, 20.0, 0.0, 40.0, AgentStatus.Critical)]
    [DataRow(50, 0.0, 0.0, 10.0, 40.0, AgentStatus.Warning)]
    [DataRow(50, 0.0, 0.0, 30.0, 40.0, AgentStatus.Critical)]
    [DataRow(50, 0.0, 0.0, 0.0, 80.0, AgentStatus.Warning)]
    [DataRow(50, 0.0, 0.0, 0.0, 92.0, AgentStatus.Critical)]
    public async Task EachResourceNeedsTwoMinutesAtItsInclusiveBoundary(int idle, double cpu, double memory, double io, double disk, AgentStatus expected)
    {
        _agent.UsedRoot = disk;
        await Run(110, idle, cpu, memory, io);
        Assert.AreEqual(AgentStatus.Unknown, Status);
        await Run(10, idle, cpu, memory, io);
        Assert.AreEqual(expected, Status);
    }

    [TestMethod]
    public async Task QuietSpikesDoNotClearAnIncidentAndFiveMinutesOfRealRecoveryDo()
    {
        await Run(120, memory: 25);
        Assert.AreEqual(AgentStatus.Critical, Status);
        for (var i = 0; i < 20; i++)
        {
            await Run(10);
            await Run(50, memory: 25);
            Assert.AreEqual(AgentStatus.Critical, Status);
        }
        _clock.Now = _clock.Now.AddSeconds(10);
        await Run(290);
        Assert.AreEqual(AgentStatus.Critical, Status);
        await Run(10);
        Assert.AreEqual(AgentStatus.Healthy, Status);
    }

    [TestMethod]
    public async Task RedDropsToYellowWithoutPretendingToRecover()
    {
        await Run(120, memory: 20);
        _clock.Now = _clock.Now.AddSeconds(10);
        await Run(290, memory: 6);
        Assert.AreEqual(AgentStatus.Critical, Status);
        await Run(10, memory: 6);
        Assert.AreEqual(AgentStatus.Warning, Status);
        await Run(1000, memory: 3);
        Assert.AreEqual(AgentStatus.Warning, Status);
    }

    [TestMethod]
    public async Task DuplicateReportsAndReadingThePageCannotAdvanceRecovery()
    {
        await Run(120, memory: 20);
        var at = _clock.Now.AddSeconds(10);
        _clock.Now = at;
        for (var i = 0; i < 1000; i++) { await Sample(at: at); Assert.AreEqual(AgentStatus.Critical, Status); }
        _clock.Now = _clock.Now.AddMinutes(6);
        _agent.LastUpdate = _clock.Now;
        Assert.AreEqual(AgentStatus.Unknown, Status);
        await Sample(at: at);
        Assert.AreEqual(AgentStatus.Unknown, Status);
    }

    [TestMethod]
    public async Task MissingCoverageRebootAndMissingPsiRequireRecoveryAgain()
    {
        await Run(300);
        Assert.AreEqual(AgentStatus.Healthy, Status);
        _clock.Now = _clock.Now.AddSeconds(30);
        await Sample();
        Assert.AreEqual(AgentStatus.Unknown, Status);
        await Run(300);
        Assert.AreEqual(AgentStatus.Healthy, Status);
        _agent.BootTime = _clock.Now;
        await Sample();
        Assert.AreEqual(AgentStatus.Unknown, Status);
        await Run(300);
        await _agent.Stats.BroadcastAsync(new DstatResult { SampleTime = _clock.Now, CpuIdl = 100 });
        Assert.AreEqual(AgentStatus.Unknown, Status);
    }

    [TestMethod]
    public async Task BootTimeEstimationJitterDoesNotResetConfirmedHealth()
    {
        _agent.BootTime = _clock.Now.AddDays(-1);
        await Run(300);
        _agent.BootTime = _agent.BootTime.AddMilliseconds(200);
        _clock.Now = _clock.Now.AddSeconds(10);
        await Sample();
        Assert.AreEqual(AgentStatus.Healthy, Status);
    }

    [TestMethod]
    public async Task BadDiskAndFutureSamplesCannotEstablishHealth()
    {
        _agent.TotalRoot = 0;
        await Run(600);
        Assert.AreEqual(AgentStatus.Unknown, Status);
        _agent.TotalRoot = 100;
        await Sample(at: _clock.Now.AddMinutes(1));
        Assert.AreEqual(AgentStatus.Unknown, Status);
        _agent.Disks = [new DiskSpaceInfo { Name = "/data", Total = 100, Used = 95 }];
        await Run(120);
        Assert.AreEqual(AgentStatus.Critical, Status);
        Assert.AreEqual(0.95, _agent.GetHealthReport().DiskUseRatio);
    }

    [TestMethod]
    public async Task CpuIdleUsesElapsedTimeSmoothing()
    {
        await Sample(idle: 100);
        await Run(60, idle: 0, cpu: 100);
        Assert.AreEqual(100 * Math.Exp(-1), _agent.GetHealthReport().SmoothedIdle!.Value, 0.001);
        Assert.AreNotEqual(AgentStatus.Critical, Status);
    }

    [TestMethod]
    public void PressureParserUsesSomeNotFullAndInvariantDecimals()
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.AreEqual(12.34, PressureReader.ParseSomeAverage("full avg60=99.9\nsome avg10=0 avg60=12.34 avg300=1 total=123\n"));
            foreach (var text in new[] { "full avg60=0", "some avg60=NaN", "some avg60=101", "some avg60=-1", "" })
                Assert.IsNull(PressureReader.ParseSomeAverage(text));
        }
        finally { CultureInfo.CurrentCulture = old; }
    }
}
