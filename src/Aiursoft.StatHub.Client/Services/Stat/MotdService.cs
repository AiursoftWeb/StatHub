using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class MotdService
{
    private readonly CacheService _cacheService;

    public MotdService(
        CacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task<string?> GetMotdFirstLine()
    {
        return _cacheService.RunWithCache("motd", async () =>
        {
            var motd = await File.ReadAllTextAsync("/etc/motd");

            var firstLine = motd
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
            return firstLine;
        }, cachedMinutes: _ => TimeSpan.FromMinutes(20))!;
    }
}