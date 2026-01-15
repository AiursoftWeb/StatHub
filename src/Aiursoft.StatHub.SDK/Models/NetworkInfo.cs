namespace Aiursoft.StatHub.SDK.Models;

public class NetworkInfo(long recv, long send)
{
    public long Recv { get; set; } = recv;
    public long Send { get; set; } = send;
}