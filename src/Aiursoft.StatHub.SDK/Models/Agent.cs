using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.DefaultConsumers;

namespace Aiursoft.StatHub.SDK.Models;

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

    public string ClientId { get; set; }
    public string Hostname { get; set; } = null!;
    public string OsName { get; set; } = null!;
    public string? KernelVersion { get; set; }
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
        recv.count > 0 ? recv.Sum / recv.count : 0,
        send.count > 0 ? send.Sum / send.count : 0);
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
        read.count > 0 ? read.Sum / read.count : 0,
        writ.count > 0 ? writ.Sum / writ.count : 0);
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
    public string? CountryName { get; set; }
    public string? CountryCode { get; set; }
    public string? Motd { get; set; }

    public List<ContainerInfo> Containers { get; set; } = new();

    public string GetSku()
    {
        return $"{CpuCores}C-{RamInGb}G-{TotalRoot}G";
    }

    public int GetSkuInNumber()
    {
        return CpuCores * 1000
               + RamInGb;
    }

    public bool IsPrivateIp()
    {
        if (string.IsNullOrWhiteSpace(Ip))
            return true;

        // Check for localhost
        if (Ip == "::1" || Ip == "127.0.0.1" || Ip.StartsWith("127."))
            return true;

        // Parse IP address
        if (System.Net.IPAddress.TryParse(Ip, out var ipAddress))
        {
            var bytes = ipAddress.GetAddressBytes();

            // IPv4 private ranges
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // 10.0.0.0 - 10.255.255.255
                if (bytes[0] == 10)
                    return true;

                // 172.16.0.0 - 172.31.255.255
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    return true;

                // 192.168.0.0 - 192.168.255.255
                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;
            }
            // IPv6 private ranges
            else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // Check for link-local (fe80::/10)
                if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
                    return true;

                // Check for unique local (fc00::/7)
                if ((bytes[0] & 0xfe) == 0xfc)
                    return true;
            }
        }

        return false;
    }


    /// <summary>
    /// 计算并返回 Agent 的完整健康报告。
    /// </summary>
    public AgentHealthReport GetHealthReport()
    {
        // 1. 核心计算
        var isOutDated = LastUpdate < DateTime.UtcNow.AddMinutes(-1);

        var load = GetLoad();
        // Normalize load by CPU cores and convert to percentage
        // loadPerCore represents utilization: 1.0 = 100% of one core
        var loadPerCore = CpuCores > 0 ? load.Load15M / CpuCores : 0;
        var loadRate = loadPerCore * 100; // Convert to percentage for consistency

        var cpu = GetCpuUsage();
        var cpuRate = cpu.Ratio;

        var diskUseRatio = UsedRoot > 0 && TotalRoot > 0 ?
            UsedRoot / (double)TotalRoot : 0;

        // 2. 状态逻辑
        AgentStatus status;
        string reason;

        if (isOutDated)
        {
            status = AgentStatus.Offline;
            reason = "Server is out of sync.";
        }
        else if (loadRate > 100 || cpuRate > 40 || diskUseRatio > 0.7)
        {
            status = AgentStatus.Critical;
            reason = (loadRate > 100 ? "Load critical. " : "") +
                     (cpuRate > 40 ? "CPU critical. " : "") +
                     (diskUseRatio > 0.7 ? "Disk critical. " : "");
        }
        else if (loadRate > 70 || cpuRate > 20 || diskUseRatio > 0.6)
        {
            status = AgentStatus.Warning;
            reason = (loadRate > 70 ? "Load warning. " : "") +
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