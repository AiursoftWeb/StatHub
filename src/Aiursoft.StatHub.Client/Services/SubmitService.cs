using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

public class SubmitService
{
    private readonly ExpensiveProcessService _expensiveProcessService;
    private readonly VersionService _versionService;
    private readonly CpuUsageService _cpuUsageService;
    private readonly HostnameService _hostnameService;
    private readonly UptimeService _uptimeService;
    private readonly ServerAccess _serverAccess;
    private readonly ILogger<SubmitService> _logger;

    public SubmitService(
        ExpensiveProcessService expensiveProcessService,
        VersionService versionService,
        CpuUsageService cpuUsageService,
        HostnameService hostnameService,
        UptimeService uptimeService,
        ServerAccess serverAccess,
        ILogger<SubmitService> logger)
    {
        _expensiveProcessService = expensiveProcessService;
        _versionService = versionService;
        _cpuUsageService = cpuUsageService;
        _hostnameService = hostnameService;
        _uptimeService = uptimeService;
        _serverAccess = serverAccess;
        _logger = logger;
    }
    
    public async Task SubmitAsync()
    {
        _logger.LogInformation("Gathering metrics...");
        
        var upTime = await _uptimeService.GetUpTimeAsync();
        _logger.LogInformation($"Up time: {upTime} seconds.");
        
        var hostname = await _hostnameService.GetHostnameAsync();
        _logger.LogInformation($"Hostname: {hostname}.");
        
        var cpuUsage = await _cpuUsageService.GetCpuUsageAsync();
        _logger.LogInformation($"CPU Usage: {cpuUsage}.");
        
        var version = _versionService.GetAppVersion();
        _logger.LogInformation($"Version: {version}.");
        
        var expensiveProcess = await _expensiveProcessService.GetExpensiveProcessAsync();
        _logger.LogInformation($"Expensive process: {expensiveProcess}.");

        await _serverAccess.MetricsAsync(hostname, upTime, cpuUsage, version, expensiveProcess);
    }
}