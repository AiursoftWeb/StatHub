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