using System.Runtime.InteropServices;
using Aiursoft.Canon;
using Aiursoft.CSTools.Services;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class HostnameService
{
    private readonly CacheService _cacheService;
    private readonly CommandService _commandService;

    public HostnameService(
        CacheService cacheService,
        CommandService commandService)
    {
        _cacheService = cacheService;
        _commandService = commandService;
    }

    public Task<string> GetHostnameAsync()
    {
        return _cacheService.RunWithCache("host-name", async () =>
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var hostname = await File.ReadAllTextAsync("/etc/hostname");
                return hostname.Trim();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var commandResult = await _commandService.RunCommandAsync("hostname", string.Empty, Path.GetTempPath());
                return commandResult.output.Trim();
            }

            throw new NotSupportedException("Your OS is not supported!");
        }, cachedMinutes: _ => TimeSpan.FromDays(1))!;
    }
}