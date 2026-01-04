using System.Globalization;
using Aiursoft.Canon;
using Aiursoft.CSTools.Services;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class SkuInfoService(
    CacheService cacheService,
    CommandService commandService)
{
    public Task<int> GetCpuCores()
    {
        return cacheService.RunWithCache("cpu-cores", async () =>
        {
            // In docker, this will return the number of cores of the host machine.
            var commandResult = await commandService.RunCommandAsync("nproc", "", Path.GetTempPath());
            var cores = int.Parse(commandResult.output, CultureInfo.InvariantCulture);
            return cores;
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }

    public Task<int> GetTotalRamInGb()
    {
        return cacheService.RunWithCache("total-ram", async () =>
        {
            // In docker, this will return the total ram of the host machine.
            var mem = await File.ReadAllTextAsync("/proc/meminfo");
            var totalRam = mem
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .First(t => t.StartsWith("MemTotal"))
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
            var totalRamInGb = double.Parse(totalRam, CultureInfo.InvariantCulture) / 1024 / 1024;
            
            // Hack here. Because 16 GB ram will be 15.6 GB in Linux.
            var totalRamInGbInt = Math.Ceiling(totalRamInGb);
            return (int)totalRamInGbInt;
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }

    public Task<(int total, int used)> GetRootDriveSizeInGb()
    {
        return cacheService.RunWithCache("root-drive-size", async () =>
        {
            // In docker, this will return the root drive size of the host machine.
            var rootDriveSize = await commandService.RunCommandAsync("df", "/", Path.GetTempPath());
            var rootDriveSizeInGb = double.Parse(rootDriveSize.output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture) / 1024 / 1024;
            
            // Hack here. Because 16 GB ram will be 15.6 GB in Linux.
            var rootDriveSizeInGbInt = Math.Ceiling(rootDriveSizeInGb);
            
            var rootDriveUsedInGb = double.Parse(rootDriveSize.output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[2], CultureInfo.InvariantCulture) / 1024 / 1024;
            
            // Hack here. Because 16 GB ram will be 15.6 GB in Linux.
            var rootDriveUsedInGbInt = Math.Ceiling(rootDriveUsedInGb);
            
            return ((int)rootDriveSizeInGbInt, (int)rootDriveUsedInGbInt);
        }, cachedMinutes: _ => TimeSpan.FromMinutes(10));
    }
}