using Aiursoft.AiurObserver;

namespace Aiursoft.StatHub.SDK.Models;

public class Client
{
    public string Hostname { get; set; } = null!;
    
    public string Ip { get; set; } = null!;

    public DateTime BootTime { get; set; } = DateTime.MinValue;

    public MessageStage<int> GetCpuUsage()
    {
        return Stats.Map(stat => 100 - stat.CpuIdl).StageLast();
    }

    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    
    public string Version { get; set; } = null!;
    public string Process { get; set; } = null!;
    
    public AsyncObservable<DstatResult> Stats { get; set; } = new();
}