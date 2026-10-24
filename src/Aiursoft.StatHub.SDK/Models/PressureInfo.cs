namespace Aiursoft.StatHub.SDK.Models;

/// <summary>Percent of time at least some tasks stalled on each resource (Linux PSI avg60).</summary>
public sealed record PressureInfo(double Cpu, double Memory, double Io)
{
    public bool IsValid => Valid(Cpu) && Valid(Memory) && Valid(Io);
    private static bool Valid(double value) => double.IsFinite(value) && value is >= 0 and <= 100;
}
