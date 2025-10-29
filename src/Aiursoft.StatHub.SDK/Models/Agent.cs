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
    public readonly MessageStageLast<int> CpuUsr;
    public readonly MessageStageLast<int> CpuSys;
    public readonly MessageStageLast<int> CpuIdl;
    public readonly MessageStageLast<int> CpuWai;
    public readonly MessageStageLast<int> CpuStl;

    public readonly MessageStageLast<long> MemUsed;
    public readonly MessageStageLast<long> MemBuf;
    public readonly MessageStageLast<long> MemCach;
    public readonly MessageStageLast<long> MemFree;

    private readonly MessageAdder<long> _netRecv;
    private readonly MessageAdder<long> _netSend;

    private readonly RecentMessageAverage<long> _netRecvLast30Seconds;
    private readonly RecentMessageAverage<long> _netSendLast30Seconds;

    private readonly MessageAdder<long> _diskRead;
    private readonly MessageAdder<long> _diskWrit;

    private readonly RecentMessageAverage<long> _diskReadLast30Seconds;
    private readonly RecentMessageAverage<long> _diskWritLast30Seconds;

    public readonly MessageStageLast<double> Load1M;
    public readonly MessageStageLast<double> Load5M;
    public readonly MessageStageLast<double> Load15M;

    public Agent(string clientId)
    {
        ClientId = clientId;
        Stats = new AsyncObservable<DstatResult>();

        // NOTE: 更新构造函数以初始化公共字段
        CpuUsr = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuUsr).Subscribe(CpuUsr);
        CpuSys = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuSys).Subscribe(CpuSys);
        CpuIdl = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuIdl).Subscribe(CpuIdl);
        CpuWai = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuWai).Subscribe(CpuWai);
        CpuStl = new MessageStageLast<int>();
        Stats.Map(stat => stat.CpuStl).Subscribe(CpuStl);

        MemUsed = new MessageStageLast<long>();
        Stats.Map(stat => stat.MemUsed).Subscribe(MemUsed);
        MemBuf = new MessageStageLast<long>();
        Stats.Map(stat => stat.MemBuf).Subscribe(MemBuf);
        MemCach = new MessageStageLast<long>();
        Stats.Map(stat => stat.MemCach).Subscribe(MemCach);
        MemFree = new MessageStageLast<long>();
        Stats.Map(stat => stat.MemFree).Subscribe(MemFree);

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

        Load1M = new MessageStageLast<double>();
        Stats.Map(stat => stat.Load1M).Subscribe(Load1M);
        Load5M = new MessageStageLast<double>();
        Stats.Map(stat => stat.Load5M).Subscribe(Load5M);
        Load15M = new MessageStageLast<double>();
        Stats.Map(stat => stat.Load15M).Subscribe(Load15M);
    }

    public string ClientId { get; set; } = null!;
    public string Hostname { get; set; } = null!;
    public string OsName { get; set; } = null!;
    public string Ip { get; set; } = null!;

    public DateTime BootTime { get; set; } = DateTime.MinValue;

    public CpuInfo GetCpuUsage()
    {
        return new CpuInfo(CpuUsr.Stage, CpuSys.Stage, CpuIdl.Stage, CpuWai.Stage, CpuStl.Stage);
    }

    public MemoryInfo GetMemUsed()
    {
        return new MemoryInfo(MemUsed.Stage, MemBuf.Stage, MemCach.Stage, MemFree.Stage);
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
        return new LoadInfo(Load1M.Stage, Load5M.Stage, Load15M.Stage);
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

    /// <summary>
    /// 计算并返回 Agent 的完整健康报告。
    /// </summary>
    public AgentHealthReport GetHealthReport()
    {
        // 1. 核心计算
        var isOutDated = LastUpdate < DateTime.UtcNow.AddMinutes(-1);

        var load = GetLoad();
        var loadRate = load.Load15M * 30;

        var cpu = GetCpuUsage();
        var cpuRate = cpu.Ratio;

        var diskUseRatio = UsedRoot > 0 && TotalRoot > 0 ?
            UsedRoot / (double)TotalRoot : 0;

        // 2. 状态逻辑
        var status = AgentStatus.Healthy;
        string reason;

        if (isOutDated)
        {
            status = AgentStatus.Offline;
            reason = "Server is out of sync.";
        }
        else if (loadRate > 60 || cpuRate > 40 || diskUseRatio > 0.7)
        {
            status = AgentStatus.Critical;
            reason = (loadRate > 60 ? "Load critical. " : "") +
                     (cpuRate > 40 ? "CPU critical. " : "") +
                     (diskUseRatio > 0.7 ? "Disk critical. " : "");
        }
        else if (loadRate > 30 || cpuRate > 20 || diskUseRatio > 0.6)
        {
            status = AgentStatus.Warning;
            reason = (loadRate > 30 ? "Load warning. " : "") +
                     (cpuRate > 20 ? "CPU warning. " : "") +
                     (diskUseRatio > 0.6 ? "Disk warning. " : "");
        }
        else
        {
            status = AgentStatus.Healthy;
            reason = "System healthy.";
        }

        // 3. Tooltips
        var loadPrompt = $"Load:\n1 min: {load.Load1M}\n5 min: {load.Load5M}\n15 min: {load.Load15M}";
        var cpuPrompt = $"CPU Usage:\nUser: {cpu.Usr}%\nSystem: {cpu.Sys}%\nIdle: {cpu.Idl}%\nWait: {cpu.Wai}%\nSteal: {cpu.Stl}%";
        var diskPrompt = $"{UsedRoot}GB / {TotalRoot}GB";

        // 4. 返回报告
        return new AgentHealthReport
        {
            LoadRate = loadRate,
            CpuRate = cpuRate,
            DiskUseRatio = diskUseRatio,
            Status = status,
            Reason = reason,
            LoadPrompt = loadPrompt,
            CpuPrompt = cpuPrompt,
            DiskPrompt = diskPrompt
        };
    }
}

/// <summary>
/// 定义 Agent 的健康状态。
/// </summary>
public enum AgentStatus
{
    Healthy,
    Warning,
    Critical,
    Offline
}

/// <summary>
/// 封装所有 Agent 健康状态的计算逻辑和表示逻辑。
/// </summary>
public record AgentHealthReport
{
    // 1. 核心计算指标
    public double LoadRate { get; init; }
    public int CpuRate { get; init; }
    public double DiskUseRatio { get; init; }
    public AgentStatus Status { get; init; }
    public string Reason { get; init; }

    // 2. Tooltips (用于主页)
    public string LoadPrompt { get; init; }
    public string CpuPrompt { get; init; }
    public string DiskPrompt { get; init; }

    // 3. 模板样式 (用于主页进度条)
    public string LoadColorClass => LoadRate < 10 ? "bg-success" :
                                    LoadRate < 15 ? "bg-info" :
                                    LoadRate < 30 ? "bg-secondary" :
                                    LoadRate < 60 ? "bg-warning" : "bg-danger";

    public string CpuColorClass => CpuRate < 5 ? "bg-success" :
                                   CpuRate < 10 ? "bg-info" :
                                   CpuRate < 20 ? "bg-secondary" :
                                   CpuRate < 40 ? "bg-warning" : "bg-danger";

    public string DiskColorClass => DiskUseRatio < 0.6 ? "bg-success" :
                                    DiskUseRatio < 0.7 ? "bg-warning" : "bg-danger";

    // 4. 模板样式 (用于详情页卡片 和 主页状态图标)
    public string BadgeClass => Status switch
    {
        AgentStatus.Offline => "badge-subtle-secondary",
        AgentStatus.Critical => "badge-subtle-danger",
        AgentStatus.Warning => "badge-subtle-warning",
        _ => "badge-subtle-success"
    };

    public string BadgeText => Status.ToString();

    public string IconClass => Status switch
    {
        AgentStatus.Offline => "text-muted",
        AgentStatus.Critical => "text-danger",
        AgentStatus.Warning => "text-warning",
        _ => "text-success"
    };

    public string DetailsIconDataLucide => Status switch
    {
        AgentStatus.Offline => "server-off",
        AgentStatus.Critical => "alert-triangle",
        AgentStatus.Warning => "alert-circle",
        _ => "server"
    };

    public string IndexIconDataLucide => Status switch
    {
        AgentStatus.Offline => "activity",
        AgentStatus.Critical => "alert-triangle",
        AgentStatus.Warning => "alert-circle",
        _ => "check-circle"
    };
}
