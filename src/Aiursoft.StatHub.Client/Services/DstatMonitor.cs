

using System.Diagnostics;
using Aiursoft.AiurObserver;

namespace Aiursoft.StatHub.Client.Services;

public class DstatMonitor : AsyncObservable<DstatResult>
{
    public Task Monitor()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dstat",
                Arguments = "--cpu --mem --disk --net --load --nocolor  --noheaders",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetTempPath(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.Start();

        async Task MonitorOutputTask()
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                if (line.StartsWith("----") || line.StartsWith("usr"))
                {
                    continue;
                }
                if (line.Replace("|", " ").Split(" ", StringSplitOptions.RemoveEmptyEntries).Length < 16)
                {
                    Console.WriteLine("Invalid line:");
                    Console.WriteLine(line);
                    continue;
                }
                try
                {
                    var result = new DstatResult(line);
                    await BroadcastAsync(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine(line);
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    throw;
                }
            }
        }

        return Task.WhenAll(
            MonitorOutputTask(),
            process.WaitForExitAsync()
        );
    }
}

public static class DstatNumberProcessor
{
    public static long ParseDataSize(string number)
    {
        if (number.EndsWith("B"))
        {
            return long.Parse(number.Replace("B", ""));
        }
        if (number.EndsWith("k"))
        {
            return long.Parse(number.Replace("k", "")) * 1024;
        }
        if (number.EndsWith("M"))
        {
            return long.Parse(number.Replace("M", "")) * 1024 * 1024;
        }
        if (number.EndsWith("G"))
        {
            return long.Parse(number.Replace("G", "")) * 1024 * 1024 * 1024;
        }
        return long.Parse(number);
    }
}

public class DstatResult
{
    public DstatResult(string line)
    {
        var parts = line
            .Replace("|", " ")
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