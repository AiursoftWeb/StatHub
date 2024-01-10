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

    public Task<string> GetMotdLast10Lines()
    {
        return _cacheService.RunWithCache("motd", async () =>
        {
            var motd = await File.ReadAllTextAsync("/etc/motd");
            var lines = motd.Split('\n');
            var last10Lines = string.Join('\n', lines.TakeLast(10));
            return last10Lines;
        }, cachedMinutes: _ => TimeSpan.FromMinutes(20))!;
    }
}