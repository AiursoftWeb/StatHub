using System.Globalization;
using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class BootTimeService
{
    private readonly CacheService _cacheService;

    public BootTimeService(
        CacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task<DateTime> GetBootTimeAsync()
    {
        return _cacheService.RunWithCache("boot-time", async () =>
        {
            var uptime = await File.ReadAllTextAsync("/proc/uptime");
            var upSeconds = double.Parse(uptime.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0],
                CultureInfo.InvariantCulture);

            return DateTime.UtcNow - TimeSpan.FromSeconds(upSeconds);
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }
}