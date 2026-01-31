namespace Aiursoft.StatHub.SDK.Models;

public class ContainerInfo
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Image { get; set; } = null!;
    public string State { get; set; } = null!;
    public string Status { get; set; } = null!;
    public double CpuPercentage { get; set; }
    public long MemoryUsage { get; set; }
    public long MemoryLimit { get; set; }
    public string Uptime { get; set; } = null!;
    public DateTime CreatedTime { get; set; }
    public string Ports { get; set; } = null!;
    public bool IsHealthy { get; set; }
    public bool HasHealthCheck { get; set; }
}
