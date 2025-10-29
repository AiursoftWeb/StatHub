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

public class CpuInfo(int usr, int sys, int idl, int wai, int stl)
{
    public int Usr { get; set; } = usr;
    public int Sys { get; set; } = sys;
    public int Idl { get; set; } = idl;
    public int Wai { get; set; } = wai;
    public int Stl { get; set; } = stl;

    public readonly int Ratio = 100 - idl;
}

public class LoadInfo(double load1M, double load5M, double load15M)
{
    public double Load1M { get; set; } = load1M;
    public double Load5M { get; set; } = load5M;
    public double Load15M { get; set; } = load15M;
}

public class NetworkInfo(long recv, long send)
{
    public long Recv { get; set; } = recv;
    public long Send { get; set; } = send;
}

public class DiskInfo(long read, long writ)
{
    public long Read { get; set; } = read;
    public long Writ { get; set; } = writ;
}

public class Agent
{
    private readonly MessageStageLast<int> _cpuUsr;
    private readonly MessageStageLast<int> _cpuSys;
    public readonly MessageStageLast<int> CpuIdl;
    private readonly MessageStageLast<int> _cpuWai;
    private readonly MessageStageLast<int> _cpuStl;

    public readonly MessageStageLast<long> MemUsed;
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

    public Agent(string clientId)
    {
        ClientId = clientId;
        Stats = new AsyncObservable<DstatResult>();
        _cpuUsr = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuUsr).Subscribe(_cpuUsr);
        _cpuSys = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuSys).Subscribe(_cpuSys);
        CpuIdl = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuIdl).Subscribe(CpuIdl);
        _cpuWai = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuWai).Subscribe(_cpuWai);
        _cpuStl = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuStl).Subscribe(_cpuStl);

        MemUsed = new MessageStageLast<long>();
        Stats.Map(stat => stat.MemUsed).Subscribe(MemUsed);
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
        return new CpuInfo(_cpuUsr.Stage, _cpuSys.Stage, CpuIdl.Stage, _cpuWai.Stage, _cpuStl.Stage);
    }

    public MemoryInfo GetMemUsed()
    {
        return new MemoryInfo(MemUsed.Stage, _memBuf.Stage, _memCach.Stage, _memFree.Stage);
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
