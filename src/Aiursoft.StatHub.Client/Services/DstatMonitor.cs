

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
// 0   0  99   0   0|  41G   20G  701M  714M| 108k    0 |  34M  919k|0.69 1.04 1.51
// 0   4  96   0   0|  41G   20G  701M  714M| 624k  173M|  17M  766k|0.69 1.04 1.51
// 0   0  99   0   0|  41G   20G  701M  714M|   0  4639k|3070k  161k|0.69 1.04 1.51
// 0   0 100   0   0|  41G   20G  701M  714M|2262k    0 |5212k  478k|0.69 1.04 1.51
// 0   0 100   0   0|  41G   20G  701M  714M|2136k    0 |  18M 1022k|0.63 1.02 1.50
// 0   0 100   0   0|  41G   20G  701M  714M|1637k    0 |  16M 1332k|0.63 1.02 1.50
// 0   5  95   0   0|  41G   20G  701M  714M|1315k  232M|9225k  314k|0.63 1.02 1.50
// 0   0  99   0   0|  41G   20G  701M  714M|   0  4751k|3911k  277k|0.63 1.02 1.50
// 0   0  99   0   0|  41G   20G  701M  714M|8119k  192k|6789k 7292k|0.63 1.02 1.50
// 0   0 100   0   0|  41G   20G  701M  714M| 688k    0 |1111k  307k|0.58 1.00 1.49
// 0   0 100   0   0|  41G   20G  701M  714M|2254k    0 |5430k  722k|0.58 1.00 1.49
// 0   4  94   1   0|  41G   20G  701M  714M|5233k  207M|2639k 3810k|0.58 1.00 1.49
// 0   1  94   4   0|  41G   20G  701M  714M| 236k   45M|1419k   72k|0.58 1.00 1.49
// 0   3  93   4   0|  41G   20G  701M  714M| 360k   95M|8945k  246k|0.58 1.00 1.49
// 0   1  98   1   0|  41G   20G  701M  714M| 488k   20M|5104k  286k|0.53 0.99 1.48

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
            .Replace("|", "")
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