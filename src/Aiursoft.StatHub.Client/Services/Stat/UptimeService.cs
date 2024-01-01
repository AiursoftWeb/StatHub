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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var uptime = await File.ReadAllTextAsync("/proc/uptime");
                var upSeconds = double.Parse(uptime.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0],
                    CultureInfo.InvariantCulture);

                return DateTime.UtcNow - TimeSpan.FromSeconds(upSeconds);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var commandResult =
                    await _commandService.RunCommandAsync("systeminfo", string.Empty, Path.GetTempPath());
                var upTime = commandResult.output.Split("\n").FirstOrDefault(t => t.StartsWith("System Boot Time:"));
                var uptimeDateString = upTime!.Substring("System Boot Time:".Length).Trim();
                var upTimeDate = DateTime.Parse(uptimeDateString, CultureInfo.InvariantCulture);
                return upTimeDate;
            }

            throw new NotSupportedException("Your OS is not supported!");
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }
}