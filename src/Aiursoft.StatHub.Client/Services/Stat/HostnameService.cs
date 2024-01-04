using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class HostnameService
{
    private readonly CacheService _cacheService;

    public HostnameService(
        CacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task<string> GetHostnameAsync()
    {
        return _cacheService.RunWithCache("host-name", async () =>
        {
                var hostname = await File.ReadAllTextAsync("/etc/hostname");
                return hostname.Trim();
        }, cachedMinutes: _ => TimeSpan.FromDays(1))!;
    }
}

public class OsInfoService
{
    private readonly CacheService _cacheService;

    public OsInfoService(
        CacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task<string> GetOsInfoAsync()
    {
        return _cacheService.RunWithCache("host-name", async () =>
        {
            var osInfo = await File.ReadAllTextAsync("/etc/os-release");

            var prettyName = osInfo
                .Split('\n')
                .FirstOrDefault(l => l.StartsWith("PRETTY_NAME="))?
                .Split('=')[1] ?? "Unknown";
            
            return prettyName.Trim();
        }, cachedMinutes: _ => TimeSpan.FromDays(1))!;
    }
}