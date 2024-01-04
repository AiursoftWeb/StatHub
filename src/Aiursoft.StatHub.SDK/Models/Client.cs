using Aiursoft.AiurObserver;

namespace Aiursoft.StatHub.SDK.Models;

public class MemoryInfo
{
    public MemoryInfo(long used, long buf, long cach, long free)
    {
        Used = used;
        Buf = buf;
        Cache = cach;
        Free = free;
        var total = used + buf + cach + free;
        UsedRate = (int)(used * 100.0 / total);
        BufRate = (int)(buf * 100.0 / total);
        CacheRate = (int)(cach * 100.0 / total);
        FreeRate = (int)(free * 100.0 / total);
    }
    
    public int UsedRate { get; set; }
    public long Used { get; set; }
    
    public int BufRate { get; set; }
    public long Buf { get; set; }
    
    public int CacheRate { get; set; }
    public long Cache { get; set; }
    
    public int FreeRate { get; set; }
    public long Free { get; set; }
}

public class Client
{
    private readonly MessageStage<int> _cpuUsage;
    private readonly MessageStage<long> _memUsed;
    private readonly MessageStage<long> _memBuf;
    private readonly MessageStage<long> _memCach;
    private readonly MessageStage<long> _memFree;
    
    public Client()
    {
        Stats = new AsyncObservable<DstatResult>();
        _cpuUsage = Stats.Map(stat => 100 - stat.CpuIdl).StageLast();
        _memUsed = Stats.Map(stat => stat.MemUsed).StageLast();
        _memBuf = Stats.Map(stat => stat.MemBuf).StageLast();
        _memCach = Stats.Map(stat => stat.MemCach).StageLast();
        _memFree = Stats.Map(stat => stat.MemFree).StageLast();
    }
    
    public string Hostname { get; set; } = null!;
    
    public string Ip { get; set; } = null!;

    public DateTime BootTime { get; set; } = DateTime.MinValue;

    public int GetCpuUsage()
    {
        return _cpuUsage.Stage;
    }
    
    public MemoryInfo GetMemUsed()
    {
        return new MemoryInfo(_memUsed.Stage, _memBuf.Stage, _memCach.Stage, _memFree.Stage);
    }

    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    
    public string Version { get; set; } = null!;
    public string Process { get; set; } = null!;
    
    public AsyncObservable<DstatResult> Stats { get; set; }
}