namespace Aiursoft.StatHub.SDK.Models;

public class Client
{
    public string Hostname { get; set; } = null!;
    
    public string Ip { get; set; } = null!;

    public DateTime BootTime { get; set; } = DateTime.MinValue;
    
    public int CpuUsage { get; set; }
    
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    
    public string Version { get; set; } = null!;
    public string Process { get; set; } = null!;
}