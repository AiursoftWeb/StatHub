using Aiursoft.StatHub.SDK.Models;
using Aiursoft.StatHub.Services.MetricsExport;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Aiursoft.StatHub.Tests;

[TestClass]
public class MetricsExportTests
{
    private static MetricsExportBuffer Buffer(bool enabled = true, int capacity = 100) =>
        new(Options.Create(new MetricsExportOptions { Enabled = enabled, MaxBufferedSamples = capacity }));

    [TestMethod]
    public async Task DisabledAndEmptyExportDoesNotWrite()
    {
        var disabled = Buffer(enabled: false);
        disabled.Capture("server", "host", new DstatResult(), DateTime.UtcNow);
        Assert.AreEqual(0, disabled.Count);
        Assert.AreEqual(0, await disabled.FlushAsync(_ => throw new AssertFailedException("Disabled export must not write.")));
        Assert.AreEqual(0, await Buffer().FlushAsync(_ => throw new AssertFailedException("Empty export must not write.")));
    }

    [TestMethod]
    public async Task EveryRawSampleAndMetricIsPreserved()
    {
        var buffer = Buffer();
        var first = new DstatResult("1 2 90 3 4 100M 200M 30M 40M 50M 60M 70M 80M 0.1 0.2 0.3");
        var second = new DstatResult("5 6 80 7 2 110M 210M 31M 41M 51M 61M 71M 81M 0.4 0.5 0.6");
        var firstTime = new DateTime(2026, 10, 21, 8, 0, 1, DateTimeKind.Utc);
        first.SampleTime = firstTime;
        second.SampleTime = firstTime.AddSeconds(1);
        var received = firstTime.AddSeconds(10);
        buffer.Capture("server", "host", first, received);
        buffer.Capture("server", "host", second, received);
        // Receiving later metrics must not mutate already captured history.
        first.CpuUsr = 99;
        var exported = await buffer.FlushAsync(batch =>
        {
            Assert.AreEqual(2, batch.Count);
            var row = batch[0];
            Assert.AreEqual(first.SampleId, row.SampleId);
            Assert.AreEqual("server", row.ClientId);
            Assert.AreEqual("host", row.Hostname);
            Assert.AreEqual(firstTime, row.SampleTime);
            Assert.AreEqual(received, row.ReceivedTime);
            Assert.IsTrue(row.HasSampleTime);
            Assert.AreEqual(1, row.CpuUsr);
            Assert.AreEqual(2, row.CpuSys);
            Assert.AreEqual(90, row.CpuIdl);
            Assert.AreEqual(3, row.CpuWai);
            Assert.AreEqual(4, row.CpuStl);
            Assert.AreEqual(first.MemUsed, row.MemUsed);
            Assert.AreEqual(first.MemFree, row.MemFree);
            Assert.AreEqual(first.MemBuf, row.MemBuf);
            Assert.AreEqual(first.MemCach, row.MemCach);
            Assert.AreEqual(first.DskRead, row.DskRead);
            Assert.AreEqual(first.DskWrit, row.DskWrit);
            Assert.AreEqual(first.NetRecv, row.NetRecv);
            Assert.AreEqual(first.NetSend, row.NetSend);
            Assert.AreEqual(0.1, row.Load1M);
            Assert.AreEqual(0.2, row.Load5M);
            Assert.AreEqual(0.3, row.Load15M);
            Assert.AreEqual(firstTime.AddSeconds(1), batch[1].SampleTime);
            Assert.AreEqual(5, batch[1].CpuUsr);
            return Task.CompletedTask;
        });
        Assert.AreEqual(2, exported);
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public async Task OldClientsHaveExplicitReceiptTimeAndStableRetryIdentity()
    {
        var old = JsonConvert.DeserializeObject<DstatResult>("{\"CpuUsr\":5}")!;
        Assert.IsNull(old.SampleTime);
        Assert.AreEqual(Guid.Empty, old.SampleId);
        var buffer = Buffer();
        var received = DateTime.UtcNow;
        buffer.Capture("old-client", "host", old, received);
        Guid id = default;
        await Assert.ThrowsExactlyAsync<IOException>(() => buffer.FlushAsync(batch =>
        {
            Assert.IsFalse(batch[0].HasSampleTime);
            Assert.AreEqual(received, batch[0].SampleTime);
            id = batch[0].SampleId;
            Assert.AreNotEqual(Guid.Empty, id);
            throw new IOException("Unknown write result");
        }));
        Assert.AreEqual(1, buffer.Count);
        await buffer.FlushAsync(batch =>
        {
            Assert.AreEqual(id, batch[0].SampleId);
            Assert.AreEqual(received, batch[0].SampleTime);
            return Task.CompletedTask;
        });
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public async Task ReceivingDuringSuccessfulFlushDoesNotLoseNewSamples()
    {
        var buffer = Buffer();
        buffer.Capture("server", "host", new DstatResult { CpuUsr = 1 }, DateTime.UtcNow);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flushing = buffer.FlushAsync(async batch =>
        {
            Assert.AreEqual(1, batch.Count);
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;
        buffer.Capture("server", "host", new DstatResult { CpuUsr = 2 }, DateTime.UtcNow);
        release.SetResult();
        Assert.AreEqual(1, await flushing);
        Assert.AreEqual(1, buffer.Count);
        await buffer.FlushAsync(batch =>
        {
            Assert.AreEqual(2, batch[0].CpuUsr);
            return Task.CompletedTask;
        });
    }

    [TestMethod]
    public async Task CapacityIncludesInFlightSamplesAndPreservesPendingData()
    {
        var buffer = Buffer(capacity: 2);
        buffer.Capture("server", "host", new DstatResult { CpuUsr = 1 }, DateTime.UtcNow);
        buffer.Capture("server", "host", new DstatResult { CpuUsr = 2 }, DateTime.UtcNow);
        await Assert.ThrowsExactlyAsync<IOException>(() => buffer.FlushAsync(_ =>
        {
            buffer.Capture("server", "host", new DstatResult { CpuUsr = 3 }, DateTime.UtcNow);
            throw new IOException("ClickHouse unavailable");
        }));
        Assert.AreEqual(2, buffer.Count);
        Assert.AreEqual(1L, buffer.DroppedSamples);
        await buffer.FlushAsync(batch =>
        {
            CollectionAssert.AreEqual(new[] { 1, 2 }, batch.Select(sample => sample.CpuUsr).ToArray());
            return Task.CompletedTask;
        });
        buffer.Capture("server", "host", new DstatResult { CpuUsr = 4 }, DateTime.UtcNow);
        Assert.AreEqual(1, buffer.Count);
    }

    [TestMethod]
    public async Task ConcurrentFlushesDoNotExportTheSameBatchTwice()
    {
        var buffer = Buffer();
        buffer.Capture("server", "host", new DstatResult(), DateTime.UtcNow);
        var writes = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = buffer.FlushAsync(async _ =>
        {
            Interlocked.Increment(ref writes);
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;
        var second = buffer.FlushAsync(_ =>
        {
            Interlocked.Increment(ref writes);
            return Task.CompletedTask;
        });
        release.SetResult();
        await Task.WhenAll(first, second);
        Assert.AreEqual(1, writes);
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public void SamplingTimeAndIdentitySurviveTransport()
    {
        var sample = new DstatResult("1 2 97 0 0 100M 200M 0 0 0 0 0 0 0.5 0.6 0.7");
        Assert.AreNotEqual(Guid.Empty, sample.SampleId);
        Assert.IsNotNull(sample.SampleTime);
        var copy = JsonConvert.DeserializeObject<DstatResult>(JsonConvert.SerializeObject(sample))!;
        Assert.AreEqual(sample.SampleId, copy.SampleId);
        Assert.AreEqual(sample.SampleTime, copy.SampleTime);
    }
}
