using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class KernelVersionService(CacheService cacheService)
{
    public Task<string> GetKernelVersionAsync()
    {
        return cacheService.RunWithCache("kernel-version", async () =>
        {
            if (File.Exists("/proc/sys/kernel/osrelease"))
            {
                return (await File.ReadAllTextAsync("/proc/sys/kernel/osrelease")).Trim();
            }
            return "Unknown";
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }
}
