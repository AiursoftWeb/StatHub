

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
                if (line.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length < 16)
                {
                    continue;
                }
                try
                {
                    var result = new DstatResult(line);
                    await BroadcastAsync(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
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


// anduin@lab:~$ sudo dstat --cpu --mem --disk --net --load --nocolor  --noheaders
// ----total-usage---- ------memory-usage----- -dsk/total- -net/total- ---load-avg---
// usr sys idl wai stl| used  free  buf   cach| read  writ| recv  send| 1m   5m  15m
// | 776M  547M  173M   13G|           |           |0.11 0.08 0.06
// 0   1  99   0   0| 777M  545M  173M   13G|   0     0 | 192B  418B|0.11 0.08 0.06
// 0   0  99   0   0| 775M  548M  173M   13G|   0    52k| 497B  570B|0.10 0.08 0.06
// 0   0 100   0   0| 775M  548M  173M   13G|   0     0 | 132B  178B|0.10 0.08 0.06
// 0   0 100   0   0| 775M  548M  173M   13G|   0     0 |3789B  220B|0.10 0.08 0.06
// 0   0 100   0   0| 775M  548M  173M   13G|   0     0 | 132B  178B|0.10 0.08 0.06
// 0   0  99   0   0| 775M  548M  173M   13G|   0   352k| 132B  178B|0.09 0.08 0.06
// 0   0 100   0   0| 775M  548M  173M   13G|   0     0 | 132B  178B|0.09 0.08 0.06
// 0   0 100   0   0| 775M  548M  173M   13G|   0  4096B| 132B  178B|0.09 0.08 0.06
// 0   0 100   0   0| 775M  548M  173M   13G|   0     0 | 132B  178B|0.09 0.08 0.06
// 1   0  99   0   0| 784M  539M  173M   13G|   0     0 | 132B  178B|0.09 0.08 0.06
// 1   0  99   0   0| 786M  537M  173M   13G|   0    52k| 132B  178B|0.08 0.07 0.06
// 0   0  99   0   0| 782M  541M  173M   13G|   0     0 | 497B  570B|0.08 0.07 0.06
// 0   0 100   0   0| 782M  541M  173M   13G|   0   100k| 132B  178B|0.08 0.07 0.06

public class DstatResult
{
    public DstatResult(string line)
    {
        var parts = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        CpuUsr = int.Parse(parts[0]);
        CpuSys = int.Parse(parts[1]);
        CpuIdl = int.Parse(parts[2]);
        CpuWai = int.Parse(parts[3]);
        CpuStl = int.Parse(parts[4]);
        MemUsed = int.Parse(parts[5]);
        MemFree = int.Parse(parts[6]);
        MemBuf = int.Parse(parts[7]);
        MemCach = int.Parse(parts[8]);
        DskRead = int.Parse(parts[9]);
        DskWrit = int.Parse(parts[10]);
        NetRecv = int.Parse(parts[11]);
        NetSend = int.Parse(parts[12]);
        Load1M = double.Parse(parts[13]);
        Load5M = double.Parse(parts[14]);
        Load15M = double.Parse(parts[15]);
    }
    
    public int CpuUsr { get; set; }
    public int CpuSys { get; set; }
    public int CpuIdl { get; set; }
    public int CpuWai { get; set; }
    public int CpuStl { get; set; }
    
    public int MemUsed { get; set; }
    public int MemFree { get; set; }
    public int MemBuf { get; set; }
    public int MemCach { get; set; }
    
    public int DskRead { get; set; }
    public int DskWrit { get; set; }
    
    public int NetRecv { get; set; }
    public int NetSend { get; set; }
    
    public double Load1M { get; set; }
    public double Load5M { get; set; }
    public double Load15M { get; set; }
}