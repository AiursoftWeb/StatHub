using System.ComponentModel.DataAnnotations;

namespace Aiursoft.StatHub.Entities.Monitoring;

public class AgentMonitoringState
{
    [Key, MaxLength(200)] public string ClientId { get; set; } = string.Empty;
    [MaxLength(200)] public string Hostname { get; set; } = string.Empty;
    [MaxLength(10)] public string CurrentState { get; set; } = "unknown";
    public DateTime LastReportedAtUtc { get; set; }
    public DateTime StateSinceUtc { get; set; }
    public DateTime? YellowOrWorseSinceUtc { get; set; }
    public DateTime? RedOrWorseSinceUtc { get; set; }
    public Guid ObservationSessionId { get; set; }
    public DateTime? PendingRecoveryAtUtc { get; set; }
    public Guid NextReportId { get; set; } = Guid.NewGuid();
}
