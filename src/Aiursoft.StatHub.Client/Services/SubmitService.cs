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
    private readonly BootTimeService _bootTimeService;
    private readonly ServerAccess _serverAccess;
    private readonly ILogger<SubmitService> _logger;

    public SubmitService(
        ExpensiveProcessService expensiveProcessService,
        VersionService versionService,
        CpuUsageService cpuUsageService,
        HostnameService hostnameService,
        BootTimeService bootTimeService,
        ServerAccess serverAccess,
        ILogger<SubmitService> logger)
    {
        _expensiveProcessService = expensiveProcessService;
        _versionService = versionService;
        _cpuUsageService = cpuUsageService;
        _hostnameService = hostnameService;
        _bootTimeService = bootTimeService;
        _serverAccess = serverAccess;
        _logger = logger;
    }
    
    public async Task SubmitAsync()
    {
        _logger.LogInformation("Gathering metrics...");
        
        var bootTime = await _bootTimeService.GetBootTimeAsync();
        _logger.LogInformation($"Boot time: {bootTime}.");
        
        var hostname = await _hostnameService.GetHostnameAsync();
        _logger.LogInformation($"Hostname: {hostname}.");
        
        var cpuUsage = await _cpuUsageService.GetCpuUsageAsync();
        _logger.LogInformation($"CPU Usage: {cpuUsage}.");
        
        var version = _versionService.GetAppVersion();
        _logger.LogInformation($"Version: {version}.");
        
        var expensiveProcess = await _expensiveProcessService.GetExpensiveProcessAsync();
        _logger.LogInformation($"Expensive process: {expensiveProcess}.");

        await _serverAccess.MetricsAsync(hostname, bootTime, cpuUsage, version, expensiveProcess);
    }
}