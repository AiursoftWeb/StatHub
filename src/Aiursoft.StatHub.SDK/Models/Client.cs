using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.DefaultConsumers;

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
    private readonly MessageStageLast<int> _cpuUsr;
    private readonly MessageStageLast<int> _cpuSys;
    private readonly MessageStageLast<int> _cpuIdl;
    private readonly MessageStageLast<int> _cpuWai;
    private readonly MessageStageLast<int> _cpuStl;

    private readonly MessageStageLast<long> _memUsed;
    private readonly MessageStageLast<long> _memBuf;
    private readonly MessageStageLast<long> _memCach;
    private readonly MessageStageLast<long> _memFree;

    private readonly MessageAdder<long> _netRecv;
    private readonly MessageAdder<long> _netSend;
    
    private readonly RecentMessageAverage<long> _netRecvLast30Seconds;
    private readonly RecentMessageAverage<long> _netSendLast30Seconds;

    private readonly MessageAdder<long> _diskRead;
    private readonly MessageAdder<long> _diskWrit;
    
    private readonly RecentMessageAverage<long> _diskReadLast30Seconds;
    private readonly RecentMessageAverage<long> _diskWritLast30Seconds;

    private readonly MessageStageLast<double> _load1M;
    private readonly MessageStageLast<double> _load5M;
    private readonly MessageStageLast<double> _load15M;

    public Client()
    {
        Stats = new AsyncObservable<DstatResult>();
        _cpuUsr = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuUsr).Subscribe(_cpuUsr);
        _cpuSys = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuSys).Subscribe(_cpuSys);
        _cpuIdl = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuIdl).Subscribe(_cpuIdl);
        _cpuWai = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuWai).Subscribe(_cpuWai);
        _cpuStl = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuStl).Subscribe(_cpuStl);

        _memUsed = new MessageStageLast<long>();
        Stats.Map(stat => stat.MemUsed).Subscribe(_memUsed);
        _memBuf = new MessageStageLast<long>();
        Stats.Map(stat => stat.MemBuf).Subscribe(_memBuf);
        _memCach = new MessageStageLast<long>();
        Stats.Map(stat => stat.MemCach).Subscribe(_memCach);
        _memFree = new MessageStageLast<long>();
        Stats.Map(stat => stat.MemFree).Subscribe(_memFree);

        _netRecv = new MessageAdder<long>();
        Stats.Map(stat => stat.NetRecv).Subscribe(_netRecv);
        _netSend = new MessageAdder<long>();
        Stats.Map(stat => stat.NetSend).Subscribe(_netSend);
        
        _netRecvLast30Seconds = new RecentMessageAverage<long>(30);
        Stats
            .Map(stat => stat.NetRecv)
            .Subscribe(_netRecvLast30Seconds);
        _netSendLast30Seconds = new RecentMessageAverage<long>(30);
        Stats
            .Map(stat => stat.NetSend)
            .Subscribe(_netSendLast30Seconds);

        _diskRead = new MessageAdder<long>();
        Stats.Map(stat => stat.DskRead).Subscribe(_diskRead);
        _diskWrit = new MessageAdder<long>();
        Stats.Map(stat => stat.DskWrit).Subscribe(_diskWrit);
        
        _diskReadLast30Seconds = new RecentMessageAverage<long>(30);
        Stats
            .Map(stat => stat.DskRead)
            .Subscribe(_diskReadLast30Seconds);
        _diskWritLast30Seconds = new RecentMessageAverage<long>(30);
        Stats
            .Map(stat => stat.DskWrit)
            .Subscribe(_diskWritLast30Seconds);

        _load1M = new MessageStageLast<double>();
        Stats.Map(stat => stat.Load1M).Subscribe(_load1M);
        _load5M = new MessageStageLast<double>();
        Stats.Map(stat => stat.Load5M).Subscribe(_load5M);
        _load15M = new MessageStageLast<double>();
        Stats.Map(stat => stat.Load15M).Subscribe(_load15M);
    }

    public string ClientId { get; set; } = null!;
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
        return new NetworkInfo(_netRecv.Sum, _netSend.Sum);
    }
    
    public NetworkInfo GetNetworkLast30Seconds()
    {
        var recv = _netRecvLast30Seconds.Average();
        var send = _netSendLast30Seconds.Average();
        return new NetworkInfo(
        recv.count > 0 ? recv.Sum / recv.count: 0, 
        send.count > 0 ? send.Sum / send.count: 0);
    }

    public DiskInfo GetDisk()
    {
        return new DiskInfo(_diskRead.Sum, _diskWrit.Sum);
    }
    
    public DiskInfo GetDiskLast30Seconds()
    {
        var read = _diskReadLast30Seconds.Average();
        var writ = _diskWritLast30Seconds.Average();
        return new DiskInfo(
        read.count > 0 ? read.Sum / read.count: 0,
        writ.count > 0 ? writ.Sum / writ.count: 0);
    }

    public LoadInfo GetLoad()
    {
        return new LoadInfo(_load1M.Stage, _load5M.Stage, _load15M.Stage);
    }

    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

    public string Version { get; set; } = null!;
    public string Process { get; set; } = null!;

    public AsyncObservable<DstatResult> Stats { get; set; }
    public int CpuCores { get; set; }
    public int RamInGb { get; set; }
    public int TotalRoot { get; set; }
    public int UsedRoot { get; set; }
    public string? Motd { get; set; }

    public string GetSku()
    {
        return $"{CpuCores}C-{RamInGb}G-{TotalRoot}G";
    }
    
    public int GetSkuInNumber()
    {
        return CpuCores * 1000 
               + RamInGb;
    }
}