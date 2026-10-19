namespace Aiursoft.StatHub.Services.Monitoring;

// One fixed-size set of counters per known agent; no sample history or database entity.
public sealed class AgentMonitoringState
{
    public string ClientId { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string CurrentState { get; set; } = "unknown";
    public DateTime LastReportedAtUtc { get; set; }
    public DateTime StateSinceUtc { get; set; }
    public DateTime? YellowOrWorseSinceUtc { get; set; }
    public DateTime? RedOrWorseSinceUtc { get; set; }
    public DateTime? PendingRecoveryAtUtc { get; set; }
    public long RecoverySequence { get; set; }
    public Guid NextReportId { get; set; } = Guid.NewGuid();

    public AgentMonitoringState Snapshot() => (AgentMonitoringState)MemberwiseClone();
}
