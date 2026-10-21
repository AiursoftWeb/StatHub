using Aiursoft.StatHub.SDK.Models;
using Aiursoft.StatHub.Entities.MetricsExport;
using Microsoft.Extensions.Options;

namespace Aiursoft.StatHub.Services.MetricsExport;

public sealed class MetricsExportBuffer(IOptions<MetricsExportOptions> options)
{
    private readonly MetricsExportOptions _options = options.Value;
    private readonly Queue<MetricsSample> _samples = new();
    private readonly SemaphoreSlim _flushGate = new(1, 1);
    private long _droppedSamples;
    private Guid? _policyVersion;
    private bool _policyEnabled = true;
    private bool _exportAllServers = true;
    private Dictionary<string, bool> _targets = new(StringComparer.Ordinal);

    public int Count { get { lock (_samples) return _samples.Count; } }
    public long DroppedSamples => Interlocked.Read(ref _droppedSamples);

    public void Capture(string clientId, string hostname, DstatResult sample, DateTime receivedTime)
    {
        if (!_options.Enabled) return;
        lock (_samples)
        {
            if (!Includes(clientId)) return;
            // Preserve the pending batch during an outage; reject new samples at the limit.
            if (_samples.Count >= _options.MaxBufferedSamples || clientId.Length > 200 || hostname.Length > 200)
            {
                Interlocked.Increment(ref _droppedSamples);
                return;
            }
            _samples.Enqueue(MetricsSample.Capture(clientId, hostname, sample, receivedTime));
        }
    }

    // Called only while holding the export policy coordinator gate. No report queries the database.
    public async Task ApplyPolicyAsync(MetricsExportPolicy policy)
    {
        await _flushGate.WaitAsync();
        try
        {
            lock (_samples)
            {
                if (_policyVersion == policy.Version) return;
                _policyEnabled = policy.IsEnabled;
                _exportAllServers = policy.ExportAllServers;
                _targets = policy.Targets.ToDictionary(target => target.ClientId, target => target.IsExcluded, StringComparer.Ordinal);
                _policyVersion = policy.Version;
                // A completed save must also remove excluded samples waiting for a future write.
                var retained = _samples.Where(sample => Includes(sample.ClientId)).ToArray();
                _samples.Clear();
                foreach (var sample in retained) _samples.Enqueue(sample);
            }
        }
        finally { _flushGate.Release(); }
    }

    private bool Includes(string clientId) => _policyEnabled &&
        (_targets.TryGetValue(clientId, out var excluded) ? !excluded : _exportAllServers);

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
