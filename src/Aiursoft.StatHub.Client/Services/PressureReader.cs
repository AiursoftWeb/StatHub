using System.Globalization;
using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Client.Services;

public static class PressureReader
{
    public static DstatResult Capture(DstatResult sample)
    {
        try
        {
            var cpu = ParseSomeAverage(File.ReadAllText("/proc/pressure/cpu"));
            var memory = ParseSomeAverage(File.ReadAllText("/proc/pressure/memory"));
            var io = ParseSomeAverage(File.ReadAllText("/proc/pressure/io"));
            if (cpu.HasValue && memory.HasValue && io.HasValue)
                sample.Pressure = new PressureInfo(cpu.Value, memory.Value, io.Value);
        }
        catch (IOException) { /* Missing PSI must remain unknown, never zero pressure. */ }
        catch (UnauthorizedAccessException) { /* The rest of the metrics can still be reported. */ }
        return sample;
    }

    public static double? ParseSomeAverage(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 0 || fields[0] != "some") continue;
            foreach (var field in fields.Where(field => field.StartsWith("avg60=", StringComparison.Ordinal)))
                if (double.TryParse(field.AsSpan(6), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                    double.IsFinite(value) && value is >= 0 and <= 100) return value;
        }
        return null;
    }
}
