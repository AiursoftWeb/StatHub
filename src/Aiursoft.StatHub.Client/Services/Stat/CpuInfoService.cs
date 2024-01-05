using Aiursoft.Canon;
using Aiursoft.CSTools.Services;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class CpuInfoService
{
    private readonly CacheService _cacheService;
    private readonly CommandService _commandService;

    public CpuInfoService(
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
            var commandResult = await _commandService.RunCommandAsync("free", "-g", Path.GetTempPath());
            var totalRam = int.Parse(commandResult.output.Split("\n")[1].Split(" ", StringSplitOptions.RemoveEmptyEntries)[1]);
            return totalRam;
        }, cachedMinutes: _ => TimeSpan.FromDays(1));
    }
}