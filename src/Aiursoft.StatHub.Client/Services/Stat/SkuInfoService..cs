using Aiursoft.Canon;
using Aiursoft.CSTools.Services;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class SkuInfoService
{
    private readonly CacheService _cacheService;
    private readonly CommandService _commandService;

    public SkuInfoService(
        CacheService cacheService,
        CommandService commandService)
    {
        _cacheService = cacheService;
        _commandService = commandService;
    }
    
    public Task<int> GetCpuCores()
    {
        return _cacheService.RunWithCache("cpu-cores", async () =>
        {
            var commandResult = await _commandService.RunCommandAsync("nproc", "", Path.GetTempPath());
            var cores = int.Parse(commandResult.output);
            return cores;
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }

    public Task<int> GetTotalRamInGb()
    {
        return _cacheService.RunWithCache("total-ram", async () =>
        {
            var mem = await File.ReadAllTextAsync("/proc/meminfo");
            var totalRam = mem
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .First(t => t.StartsWith("MemTotal"))
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
            var totalRamInGb = double.Parse(totalRam) / 1024 / 1024;
            
            // Hack here. Because 16 GB ram will be 15.6 GB in Linux.
            var totalRamInGbInt = Math.Ceiling(totalRamInGb);
            return (int)totalRamInGbInt;
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }

    public Task<(int total, int used)> GetRootDriveSizeInGb()
    {
        return _cacheService.RunWithCache("root-drive-size", async () =>
        {
            var rootDriveSize = await _commandService.RunCommandAsync("df", "/", Path.GetTempPath());
            var rootDriveSizeInGb = double.Parse(rootDriveSize.output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]) / 1024 / 1024;
            
            // Hack here. Because 16 GB ram will be 15.6 GB in Linux.
            var rootDriveSizeInGbInt = Math.Ceiling(rootDriveSizeInGb);
            
            var rootDriveUsedInGb = double.Parse(rootDriveSize.output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[2]) / 1024 / 1024;
            
            // Hack here. Because 16 GB ram will be 15.6 GB in Linux.
            var rootDriveUsedInGbInt = Math.Ceiling(rootDriveUsedInGb);
            
            return ((int)rootDriveSizeInGbInt, (int)rootDriveUsedInGbInt);
        }, cachedMinutes: _ => TimeSpan.FromMinutes(10));
    }
}