using Aiursoft.AiurObserver;

namespace Aiursoft.StatHub.SDK.Models;

public class Client
{
    private MessageStage<int> _cpuUsage;
    private MessageStage<long> _memUsed;
    
    public Client()
    {
        Stats = new AsyncObservable<DstatResult>();
        _cpuUsage = Stats.Map(stat => 100 - stat.CpuIdl).StageLast();
        _memUsed = Stats.Map(stat => stat.MemUsed).StageLast();
    }
    
    public string Hostname { get; set; } = null!;
    
    public string Ip { get; set; } = null!;

    public DateTime BootTime { get; set; } = DateTime.MinValue;

    public int GetCpuUsage()
    {
        return _cpuUsage.Stage;
    }
    
    public long GetMemUsed()
    {
        return _memUsed.Stage;
    }

    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    
    public string Version { get; set; } = null!;
    public string Process { get; set; } = null!;
    
    public AsyncObservable<DstatResult> Stats { get; set; }
}