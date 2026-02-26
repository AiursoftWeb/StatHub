using System.Globalization;

namespace Aiursoft.StatHub.SDK.Models;

/// <summary>
/// Represents the result of a single dstat measurement.
/// </summary>
public class DstatResult
{
    /// <summary>
    /// This method is for JSON serialization.
    /// </summary>
    public DstatResult()
    {
        
    }
    
    /// <summary>
    /// Parses a line of dstat output.
    /// </summary>
    /// <param name="line">The raw dstat output line.</param>
    public DstatResult(string line)
    {
        var parts = line
            .Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 16)
        {
            return;
        }

        if (int.TryParse(parts[0], CultureInfo.InvariantCulture, out var cpuUsr)) CpuUsr = cpuUsr;
        if (int.TryParse(parts[1], CultureInfo.InvariantCulture, out var cpuSys)) CpuSys = cpuSys;
        if (int.TryParse(parts[2], CultureInfo.InvariantCulture, out var cpuIdl)) CpuIdl = cpuIdl;
        if (int.TryParse(parts[3], CultureInfo.InvariantCulture, out var cpuWai)) CpuWai = cpuWai;
        if (int.TryParse(parts[4], CultureInfo.InvariantCulture, out var cpuStl)) CpuStl = cpuStl;
        
        MemUsed = DstatNumberProcessor.ParseDataSize(parts[5]);
        MemFree = DstatNumberProcessor.ParseDataSize(parts[6]);
        MemBuf = DstatNumberProcessor.ParseDataSize(parts[7]);
        MemCach = DstatNumberProcessor.ParseDataSize(parts[8]);
        
        DskRead = DstatNumberProcessor.ParseDataSize(parts[9]);
        DskWrit = DstatNumberProcessor.ParseDataSize(parts[10]);
        
        NetRecv = DstatNumberProcessor.ParseDataSize(parts[11]);
        NetSend = DstatNumberProcessor.ParseDataSize(parts[12]);
        
        if (double.TryParse(parts[13], CultureInfo.InvariantCulture, out var load1M)) Load1M = load1M;
        if (double.TryParse(parts[14], CultureInfo.InvariantCulture, out var load5M)) Load5M = load5M;
        if (double.TryParse(parts[15], CultureInfo.InvariantCulture, out var load15M)) Load15M = load15M;
    }
    
    public int CpuUsr { get; set; }
    public int CpuSys { get; set; }
    public int CpuIdl { get; set; }
    public int CpuWai { get; set; }
    public int CpuStl { get; set; }
    
    public long MemUsed { get; set; }
    public long MemFree { get; set; }
    public long MemBuf { get; set; }
    public long MemCach { get; set; }
    
    public long DskRead { get; set; }
    public long DskWrit { get; set; }
    
    public long NetRecv { get; set; }
    public long NetSend { get; set; }
    
    public double Load1M { get; set; }
    public double Load5M { get; set; }
    public double Load15M { get; set; }
}