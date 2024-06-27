using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class OsInfoService(CacheService cacheService)
{
    public Task<string> GetOsInfoAsync()
    {
        return cacheService.RunWithCache("os-info", async () =>
        {
            // We need to pass through the /etc/lsb-release file to docker container.
            var osInfo = await File.ReadAllTextAsync("/etc/lsb-release");

            var prettyName = osInfo
                .Split('\n')
                .FirstOrDefault(l => l.StartsWith("DISTRIB_DESCRIPTION"))?
                .Split('=')[1]
                .Trim('"') ?? "Unknown";
            
            return prettyName.Trim();
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }
}