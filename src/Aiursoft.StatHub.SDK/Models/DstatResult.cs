namespace Aiursoft.StatHub.SDK.Models;

public class DstatResult
{
    public DstatResult(string line)
    {
        var parts = line
            .Split(" ", StringSplitOptions.RemoveEmptyEntries);
        CpuUsr = int.Parse(parts[0]);
        CpuSys = int.Parse(parts[1]);
        CpuIdl = int.Parse(parts[2]);
        CpuWai = int.Parse(parts[3]);
        CpuStl = int.Parse(parts[4]);
        
        MemUsed = DstatNumberProcessor.ParseDataSize(parts[5]);
        MemFree = DstatNumberProcessor.ParseDataSize(parts[6]);
        MemBuf = DstatNumberProcessor.ParseDataSize(parts[7]);
        MemCach = DstatNumberProcessor.ParseDataSize(parts[8]);
        
        DskRead = DstatNumberProcessor.ParseDataSize(parts[9]);
        DskWrit = DstatNumberProcessor.ParseDataSize(parts[10]);
        
        NetRecv = DstatNumberProcessor.ParseDataSize(parts[11]);
        NetSend = DstatNumberProcessor.ParseDataSize(parts[12]);
        
        Load1M = double.Parse(parts[13]);
        Load5M = double.Parse(parts[14]);
        Load15M = double.Parse(parts[15]);
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