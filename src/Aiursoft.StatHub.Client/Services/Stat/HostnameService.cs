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