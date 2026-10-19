using Aiursoft.StatHub.Entities.Monitoring;
using Aiursoft.UiStack.Layout;
using Aiursoft.StatHub.Services.Monitoring;

namespace Aiursoft.StatHub.Models.Monitoring;

public sealed class MonitoringViewModel : UiStackLayoutViewModel
{
    public MonitoringViewModel() => PageTitle = "AlertCenter monitoring";
    public MonitoringPolicy Policy { get; init; } = new();
    public List<AgentMonitoringState> Agents { get; init; } = [];
}

public sealed class MonitoringPolicyInput
{
    public Guid Version { get; set; }
    public bool IsEnabled { get; set; }
    public bool MonitorAllServers { get; set; }
    public bool AutoResolveOnGreen { get; set; }
    public string[] Included { get; set; } = [];
    public string[] Excluded { get; set; } = [];
}
