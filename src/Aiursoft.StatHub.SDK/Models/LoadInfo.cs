namespace Aiursoft.StatHub.SDK.Models;

public class LoadInfo(double load1M, double load5M, double load15M)
{
    public double Load1M { get; set; } = load1M;
    public double Load5M { get; set; } = load5M;
    public double Load15M { get; set; } = load15M;
}