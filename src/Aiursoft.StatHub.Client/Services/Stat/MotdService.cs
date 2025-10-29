using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class MotdService(CacheService cacheService)
{
    public Task<string?> GetMotdFirstLine()
    {
        return cacheService.RunWithCache("motd", async () =>
        {
            // We need to passthrough the /etc/motd file to docker container.
            if (!File.Exists("/etc/motd"))
            {
                return null;
            }
            
            var motd = await File.ReadAllTextAsync("/etc/motd");

            var firstLine = motd
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
            return firstLine;
        }, cachedMinutes: _ => TimeSpan.FromMinutes(20));
    }
}