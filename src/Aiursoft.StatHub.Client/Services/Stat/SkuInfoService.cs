using System.Globalization;
using Aiursoft.Canon;
using Aiursoft.CSTools.Services;
using Aiursoft.StatHub.SDK.Models;

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

    public Task<DiskSpaceInfo[]> GetDisksSpace()
    {
        return cacheService.RunWithCache("disks-space", async () =>
        {
            // In docker, this will return the disks space of the host machine if the host's / is mounted to the container's /.
            var dfResult = await commandService.RunCommandAsync("df", "-PT", Path.GetTempPath());
            var lines = dfResult.output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1);
            var results = new List<DiskSpaceInfo>();
            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 7) continue;

                var type = parts[1];
                var mountedOn = parts[6];

                if (type == "tmpfs" || type == "devtmpfs" || type == "overlay" || type == "vfat" || type == "squashfs" || type == "iso9660" || type == "efivarfs")
                {
                    continue;
                }

                if (!long.TryParse(parts[2], CultureInfo.InvariantCulture, out var totalBlocks) ||
                    !long.TryParse(parts[3], CultureInfo.InvariantCulture, out var usedBlocks))
                {
                    continue;
                }

                results.Add(new DiskSpaceInfo
                {
                    Name = mountedOn,
                    Total = (int)Math.Ceiling(totalBlocks / 1024.0 / 1024.0),
                    Used = (int)Math.Ceiling(usedBlocks / 1024.0 / 1024.0)
                });
            }
            return results.OrderBy(r => r.Name).ToArray();
        }, cachedMinutes: _ => TimeSpan.FromMinutes(10));
    }
}