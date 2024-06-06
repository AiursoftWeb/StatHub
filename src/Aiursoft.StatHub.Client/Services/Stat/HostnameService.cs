using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class HostnameService(CacheService cacheService)
{
    public Task<string> GetHostnameAsync()
    {
        return cacheService.RunWithCache("host-name", async () =>
        {
            // We need to passthrough the /etc/hostname file to docker container.
            var hostname = await File.ReadAllTextAsync("/etc/hostname");
            return hostname.Trim();
        }, cachedMinutes: _ => TimeSpan.FromDays(1))!;
    }
}