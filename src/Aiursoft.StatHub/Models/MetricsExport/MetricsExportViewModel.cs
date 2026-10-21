using Aiursoft.StatHub.Entities.MetricsExport;
using Aiursoft.StatHub.Services.Monitoring;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.StatHub.Models.MetricsExport;

public sealed class MetricsExportViewModel : UiStackLayoutViewModel
{
    public MetricsExportViewModel() => PageTitle = "ClickHouse export";
    public MetricsExportPolicy Policy { get; init; } = new();
    public bool ConnectionEnabled { get; init; }
    public List<AgentMonitoringState> Agents { get; init; } = [];
}

public sealed class MetricsExportPolicyInput
{
    public Guid Version { get; set; }
    public bool IsEnabled { get; set; }
    public bool ExportAllServers { get; set; }
    public string[] Included { get; set; } = [];
    public string[] Excluded { get; set; } = [];
}
