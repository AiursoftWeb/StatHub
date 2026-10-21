using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.StatHub.Services.MetricsExport;

namespace Aiursoft.StatHub.Services.BackgroundJobs;

public sealed class MetricsExportJob(
    MetricsExportBuffer buffer,
    MetricsClickhouseWriter writer,
    ILogger<MetricsExportJob> logger) : IBackgroundJob
{
    public string Name => "ClickHouse metrics export";
    public string Description => "Writes raw server metrics to ClickHouse in five-minute batches.";

    public async Task ExecuteAsync()
    {
        try
        {
            var count = await buffer.FlushAsync(writer.WriteAsync);
            if (count > 0) logger.LogInformation("Exported {Count} raw metrics to ClickHouse.", count);
        }
        finally
        {
            if (buffer.DroppedSamples > 0)
                logger.LogWarning("Metrics export has dropped {DroppedSamples} samples; {PendingSamples} samples remain buffered.",
                    buffer.DroppedSamples, buffer.Count);
        }
    }
}
