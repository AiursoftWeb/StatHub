namespace Aiursoft.StatHub.SDK.Models;

public class DiskInfo(long read, long writ)
{
    public long Read { get; set; } = read;
    public long Writ { get; set; } = writ;
}