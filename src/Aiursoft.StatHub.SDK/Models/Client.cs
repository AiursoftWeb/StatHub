﻿using Aiursoft.AiurObserver;

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

    public readonly int Ratio;
}

public class LoadInfo
{
    public LoadInfo(double load1M, double load5M, double load15M)
    {
        Load1M = load1M;
        Load5M = load5M;
        Load15M = load15M;
    }
    
    public double Load1M { get; set; }
    public double Load5M { get; set; }
    public double Load15M { get; set; }
}

public class NetworkInfo
{
    public NetworkInfo(long recv, long send)
    {
        Recv = recv;
        Send = send;
    }
    
    public long Recv { get; set; }
    public long Send { get; set; }
}

public class DiskInfo
{
    public DiskInfo(long read, long writ)
    {
        Read = read;
        Writ = writ;
    }
    
    public long Read { get; set; }
    public long Writ { get; set; }
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
    
    private readonly MessageStage<long> _netRecv;
    private readonly MessageStage<long> _netSend;
    
    private readonly MessageStage<long> _diskRead;
    private readonly MessageStage<long> _diskWrit;
    
    private readonly MessageStage<double> _load1M;
    private readonly MessageStage<double> _load5M;
    private readonly MessageStage<double> _load15M;
    
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
        
        _netRecv = Stats.Map(stat => stat.NetRecv).StageLast();
        _netSend = Stats.Map(stat => stat.NetSend).StageLast();
        
        _diskRead = Stats.Map(stat => stat.DskRead).StageLast();
        _diskWrit = Stats.Map(stat => stat.DskWrit).StageLast();
        
        _load1M = Stats.Map(stat => stat.Load1M).StageLast();
        _load5M = Stats.Map(stat => stat.Load5M).StageLast();
        _load15M = Stats.Map(stat => stat.Load15M).StageLast();
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
    
    public NetworkInfo GetNetwork()
    {
        return new NetworkInfo(_netRecv.Stage, _netSend.Stage);
    }
    
    public DiskInfo GetDisk()
    {
        return new DiskInfo(_diskRead.Stage, _diskWrit.Stage);
    }
    
    public LoadInfo GetLoad()
    {
        return new LoadInfo(_load1M.Stage, _load5M.Stage, _load15M.Stage);
    }

    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    
    public string Version { get; set; } = null!;
    public string Process { get; set; } = null!;
    
    public AsyncObservable<DstatResult> Stats { get; set; }
}
