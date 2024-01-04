using Aiursoft.AiurObserver;

namespace Aiursoft.StatHub.SDK.Models;

public class Client
{
    private MessageStage<int> _cpuUsage;
    public Client()
    {
        Stats = new AsyncObservable<DstatResult>();
        _cpuUsage = Stats.Map(stat => 100 - stat.CpuIdl).StageLast();
    }
    
    public string Hostname { get; set; } = null!;
    
    public string Ip { get; set; } = null!;

    public DateTime BootTime { get; set; } = DateTime.MinValue;

    public int GetCpuUsage()
    {
        return _cpuUsage.Stage;
    }

    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    
    public string Version { get; set; } = null!;
    public string Process { get; set; } = null!;
    
    public AsyncObservable<DstatResult> Stats { get; set; }
}