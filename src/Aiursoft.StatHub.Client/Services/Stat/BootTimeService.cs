using System.Globalization;
using System.Runtime.InteropServices;
using Aiursoft.Canon;
using Aiursoft.CSTools.Services;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class BootTimeService
{
    private readonly CacheService _cacheService;
    private readonly CommandService _commandService;

    public BootTimeService(
        CacheService cacheService,
        CommandService commandService)
    {
        _cacheService = cacheService;
        _commandService = commandService;
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