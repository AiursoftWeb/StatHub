namespace Aiursoft.StatHub.Services.MetricsExport;

public sealed class MetricsExportPolicyCoordinator : IDisposable
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public void Dispose() => Gate.Dispose();
}
