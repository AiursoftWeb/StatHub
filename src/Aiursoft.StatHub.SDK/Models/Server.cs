namespace Aiursoft.StatHub.SDK.Models;

public class Client
{
    public string Hostname { get; set; } = null!;
    
    public string Ip { get; set; } = null!;
    
    public int UpTime { get; set; }
    
    public double CpuUsage { get; set; }
    
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}