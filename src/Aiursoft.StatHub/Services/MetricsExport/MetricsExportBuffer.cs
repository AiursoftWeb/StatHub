using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Options;

namespace Aiursoft.StatHub.Services.MetricsExport;

public sealed class MetricsExportBuffer(IOptions<MetricsExportOptions> options)
{
    private readonly MetricsExportOptions _options = options.Value;
    private readonly Queue<MetricsSample> _samples = new();
    private readonly SemaphoreSlim _flushGate = new(1, 1);
    private long _droppedSamples;

    public int Count { get { lock (_samples) return _samples.Count; } }
    public long DroppedSamples => Interlocked.Read(ref _droppedSamples);

    public void Capture(string clientId, string hostname, DstatResult sample, DateTime receivedTime)
    {
        if (!_options.Enabled) return;
        lock (_samples)
        {
            // Preserve the pending batch during an outage; reject new samples at the limit.
            if (_samples.Count >= _options.MaxBufferedSamples || clientId.Length > 200 || hostname.Length > 200)
            {
                Interlocked.Increment(ref _droppedSamples);
                return;
            }
            _samples.Enqueue(MetricsSample.Capture(clientId, hostname, sample, receivedTime));
        }
    }

    public async Task<int> FlushAsync(Func<IReadOnlyList<MetricsSample>, Task> write)
    {
        if (!_options.Enabled) return 0;
        await _flushGate.WaitAsync();
        try
        {
            MetricsSample[] batch;
            lock (_samples) batch = _samples.ToArray();
            if (batch.Length == 0) return 0;

            // The SDK clears its own buffer before IO. Keep our copy until the whole write succeeds.
            await write(batch);
            lock (_samples)
            {
                for (var i = 0; i < batch.Length; i++) _samples.Dequeue();
            }
            return batch.Length;
        }
        finally
        {
            _flushGate.Release();
        }
    }
}
