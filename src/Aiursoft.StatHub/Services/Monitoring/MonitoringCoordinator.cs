namespace Aiursoft.StatHub.Services.Monitoring;

// Agent reports only take this short in-memory lock. Network I/O never holds it.
public sealed class MonitoringCoordinator : IDisposable
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public object StateLock { get; } = new();
    public Dictionary<string, AgentMonitoringState> States { get; } = new(StringComparer.Ordinal);

    public HashSet<string> RecoveryCandidates { get; } = new(StringComparer.Ordinal);
    public bool RecoveryWakeQueued { get; set; }

    public List<AgentMonitoringState> Snapshot()
    {
        lock (StateLock) return States.Values.Select(state => state.Snapshot()).ToList();
    }

    public void Dispose() => Gate.Dispose();
}
