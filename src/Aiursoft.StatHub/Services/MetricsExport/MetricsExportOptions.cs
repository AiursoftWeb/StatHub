using Aiursoft.ClickhouseSdk.Abstractions;

namespace Aiursoft.StatHub.Services.MetricsExport;

public sealed class MetricsExportOptions : ClickhouseOptions
{
    public int MaxBufferedSamples { get; set; } = 100_000;
}
