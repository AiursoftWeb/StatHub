namespace Aiursoft.StatHub.Services.Monitoring;

// StatHub's live agent registry is process-local. Serialize its monitoring writes and sync passes.
public sealed class MonitoringCoordinator : IDisposable
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public Guid SessionId { get; } = Guid.NewGuid();
    public void Dispose() => Gate.Dispose();
}
