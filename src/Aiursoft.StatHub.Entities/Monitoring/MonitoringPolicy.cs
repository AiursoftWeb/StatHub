using System.ComponentModel.DataAnnotations;

namespace Aiursoft.StatHub.Entities.Monitoring;

public class MonitoringPolicy
{
    public const int DefaultId = 1;
    public int Id { get; set; } = DefaultId;
    [MaxLength(100)] public string Name { get; set; } = "Default monitoring policy";
    public bool IsEnabled { get; set; } = true;
    public bool MonitorAllServers { get; set; } = true;
    public bool AutoResolveOnGreen { get; set; } = true;
    [ConcurrencyCheck] public Guid Version { get; set; } = Guid.NewGuid();
    public List<MonitoringRule> Rules { get; set; } = [];
    public List<MonitoringTarget> Targets { get; set; } = [];
}

public class MonitoringRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int PolicyId { get; set; } = MonitoringPolicy.DefaultId;
    public MonitoringPolicy Policy { get; set; } = null!;
    [Required, RegularExpression("^(yellow|red|grey)$"), MaxLength(10)] public string State { get; set; } = "yellow";
    [Range(0, 525600)] public int DurationMinutes { get; set; } = 20;
    [Required, RegularExpression("^sev[0-4]$"), MaxLength(4)] public string Severity { get; set; } = "sev3";
    public bool IsEnabled { get; set; } = true;
}

public class MonitoringTarget
{
    public int PolicyId { get; set; } = MonitoringPolicy.DefaultId;
    public MonitoringPolicy Policy { get; set; } = null!;
    [MaxLength(200)] public string ClientId { get; set; } = string.Empty;
    public bool IsExcluded { get; set; }
}
