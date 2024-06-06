using System.Globalization;
using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class BootTimeService(CacheService cacheService)
{
    public Task<DateTime> GetBootTimeAsync()
    {
        return cacheService.RunWithCache("boot-time", async () =>
        {
            // In docker, this will return the boot time of the host machine.
            var uptime = await File.ReadAllTextAsync("/proc/uptime");
            var upSeconds = double.Parse(uptime.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0],
                CultureInfo.InvariantCulture);

            return DateTime.UtcNow - TimeSpan.FromSeconds(upSeconds);
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }
}