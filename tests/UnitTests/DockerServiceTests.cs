using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aiursoft.StatHub.Tests.UnitTests;

[TestClass]
public class DockerServiceTests
{
    [TestMethod]
    public async Task CollectImmediatelyThenOmitDockerUntil120SecondsHaveElapsed()
    {
        var clock = new TestClock();
        var service = new RecordingDockerService(clock);
        var first = await service.GetContainersForReportAsync();
        Assert.IsNotNull(first);
        Assert.AreEqual(1, service.Collections);
        service.AcknowledgeContainers(first);

        for (var report = 1; report < 12; report++)
        {
            clock.Advance(TimeSpan.FromSeconds(10));
            Assert.IsNull(await service.GetContainersForReportAsync());
        }

        clock.Advance(TimeSpan.FromSeconds(9.999));
        Assert.IsNull(await service.GetContainersForReportAsync());
        Assert.AreEqual(1, service.Collections);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        var next = await service.GetContainersForReportAsync();
        Assert.IsNotNull(next);
        Assert.AreEqual(2, service.Collections);
        Assert.AreEqual("snapshot-2", next.Single().Name);
    }

    [TestMethod]
    public async Task RetryUnacknowledgedSnapshotWithoutRepeatingDockerCollection()
    {
        var clock = new TestClock();
        var service = new RecordingDockerService(clock);
        var first = await service.GetContainersForReportAsync();
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.AreSame(first, await service.GetContainersForReportAsync());
        Assert.AreEqual(1, service.Collections);

        service.AcknowledgeContainers(first);
        Assert.IsNull(await service.GetContainersForReportAsync());
    }

    [TestMethod]
    public async Task LateAcknowledgementDoesNotDiscardANewerSnapshot()
    {
        var clock = new TestClock();
        var service = new RecordingDockerService(clock);
        var first = await service.GetContainersForReportAsync();
        clock.Advance(TimeSpan.FromSeconds(120));
        var next = await service.GetContainersForReportAsync();
        service.AcknowledgeContainers(first);
        Assert.AreSame(next, await service.GetContainersForReportAsync());
        service.AcknowledgeContainers(next);
        Assert.IsNull(await service.GetContainersForReportAsync());
    }

    [TestMethod]
    public async Task EmptyContainerSnapshotIsReportedAndAcknowledged()
    {
        var clock = new TestClock();
        var service = new RecordingDockerService(clock) { Empty = true };
        var containers = await service.GetContainersForReportAsync();
        Assert.IsNotNull(containers);
        Assert.AreEqual(0, containers.Length);
        Assert.AreSame(containers, await service.GetContainersForReportAsync());
        service.AcknowledgeContainers(containers);
        Assert.IsNull(await service.GetContainersForReportAsync());
        clock.Advance(TimeSpan.FromSeconds(120));
        Assert.IsNotNull(await service.GetContainersForReportAsync());
        Assert.AreEqual(2, service.Collections);
    }

    [TestMethod]
    public async Task OverlappingReportsShareOneDockerCollection()
    {
        var clock = new TestClock();
        var service = new RecordingDockerService(clock)
        {
            Release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var first = service.GetContainersForReportAsync();
        var second = service.GetContainersForReportAsync();
        Assert.AreEqual(1, service.Collections);
        service.Release.SetResult();
        Assert.AreSame(await first, await second);
        Assert.AreEqual(1, service.Collections);
    }

    private sealed class RecordingDockerService(TestClock clock)
        : DockerService(null!, NullLogger<DockerService>.Instance, clock)
    {
        public int Collections { get; private set; }
        public bool Empty { get; init; }
        public TaskCompletionSource? Release { get; init; }

        public override async Task<ContainerInfo[]> GetDockerContainersAsync()
        {
            Collections++;
            if (Release != null) await Release.Task;
            return Empty ? [] : [new ContainerInfo { Name = $"snapshot-{Collections}" }];
        }
    }

    private sealed class TestClock : TimeProvider
    {
        private long _timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _timestamp;
        public void Advance(TimeSpan elapsed) => _timestamp += elapsed.Ticks;
    }
}
