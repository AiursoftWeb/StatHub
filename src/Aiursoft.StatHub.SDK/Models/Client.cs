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

public class CpuInfo
{
    public CpuInfo(int usr, int sys, int idl, int wai, int stl)
    {
        Usr = usr;
        Sys = sys;
        Idl = idl;
        Wai = wai;
        Stl = stl;
        Ratio = 100 - idl;
    }
    
    public int Usr { get; set; }
    public int Sys { get; set; }
    public int Idl { get; set; }
    public int Wai { get; set; }
    public int Stl { get; set; }

    public int Ratio;
}

public class Client
{
    private readonly MessageStage<int> _cpuUsr;
    private readonly MessageStage<int> _cpuSys;
    private readonly MessageStage<int> _cpuIdl;
    private readonly MessageStage<int> _cpuWai;
    private readonly MessageStage<int> _cpuStl;
    
    private readonly MessageStage<long> _memUsed;
    private readonly MessageStage<long> _memBuf;
    private readonly MessageStage<long> _memCach;
    private readonly MessageStage<long> _memFree;
    
    public Client()
    {
        Stats = new AsyncObservable<DstatResult>();
        _cpuUsr = Stats.Map(stat => stat.CpuUsr).StageLast();
        _cpuSys = Stats.Map(stat => stat.CpuSys).StageLast();
        _cpuIdl = Stats.Map(stat => stat.CpuIdl).StageLast();
        _cpuWai = Stats.Map(stat => stat.CpuWai).StageLast();
        _cpuStl = Stats.Map(stat => stat.CpuStl).StageLast();
        
        _memUsed = Stats.Map(stat => stat.MemUsed).StageLast();
        _memBuf = Stats.Map(stat => stat.MemBuf).StageLast();
        _memCach = Stats.Map(stat => stat.MemCach).StageLast();
        _memFree = Stats.Map(stat => stat.MemFree).StageLast();
    }
    
    public string Hostname { get; set; } = null!;
    public string OsName { get; set; } = null!;
    public string Ip { get; set; } = null!;

    public DateTime BootTime { get; set; } = DateTime.MinValue;

    public CpuInfo GetCpuUsage()
    {
        return new CpuInfo(_cpuUsr.Stage, _cpuSys.Stage, _cpuIdl.Stage, _cpuWai.Stage, _cpuStl.Stage);
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