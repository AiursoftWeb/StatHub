using System.Globalization;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class UptimeService
{
    public async Task<int> GetUpTimeAsync()
    {
        // get Linux uptime:
        var uptime = await File.ReadAllTextAsync("/proc/uptime");
        var upSeconds = double.Parse(uptime.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);
        return (int)upSeconds;
    }
}