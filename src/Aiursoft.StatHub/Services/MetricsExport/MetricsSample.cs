using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Services.MetricsExport;

public sealed class MetricsSample
{
    public Guid SampleId { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string Hostname { get; init; } = string.Empty;
    public DateTime SampleTime { get; init; }
    public DateTime ReceivedTime { get; init; }
    public bool HasSampleTime { get; init; }
    public int CpuUsr { get; init; }
    public int CpuSys { get; init; }
    public int CpuIdl { get; init; }
    public int CpuWai { get; init; }
    public int CpuStl { get; init; }
    public long MemUsed { get; init; }
    public long MemFree { get; init; }
    public long MemBuf { get; init; }
    public long MemCach { get; init; }
    public long DskRead { get; init; }
    public long DskWrit { get; init; }
    public long NetRecv { get; init; }
    public long NetSend { get; init; }
    public double Load1M { get; init; }
    public double Load5M { get; init; }
    public double Load15M { get; init; }

    public static MetricsSample Capture(string clientId, string hostname, DstatResult sample, DateTime receivedTime) => new()
    {
        SampleId = sample.SampleId == Guid.Empty ? Guid.NewGuid() : sample.SampleId,
        ClientId = clientId,
        Hostname = hostname,
        SampleTime = sample.SampleTime?.ToUniversalTime() ?? receivedTime,
        ReceivedTime = receivedTime,
        HasSampleTime = sample.SampleTime.HasValue,
        CpuUsr = sample.CpuUsr, CpuSys = sample.CpuSys, CpuIdl = sample.CpuIdl,
        CpuWai = sample.CpuWai, CpuStl = sample.CpuStl,
        MemUsed = sample.MemUsed, MemFree = sample.MemFree, MemBuf = sample.MemBuf, MemCach = sample.MemCach,
        DskRead = sample.DskRead, DskWrit = sample.DskWrit,
        NetRecv = sample.NetRecv, NetSend = sample.NetSend,
        Load1M = sample.Load1M, Load5M = sample.Load5M, Load15M = sample.Load15M
    };

    public object[] ToRow() =>
    [
        SampleId, ClientId, Hostname, SampleTime, ReceivedTime, HasSampleTime ? 1 : 0,
        CpuUsr, CpuSys, CpuIdl, CpuWai, CpuStl, MemUsed, MemFree, MemBuf, MemCach,
        DskRead, DskWrit, NetRecv, NetSend, Load1M, Load5M, Load15M
    ];
}
