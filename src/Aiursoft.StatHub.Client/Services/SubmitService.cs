using Aiursoft.AiurObserver;
using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

public class SubmitService : IConsumer<DstatResult[]>
{
    private readonly MotdService _motdService;
    private readonly ClientIdService _clientIdService;
    private readonly SkuInfoService _skuInfoService;
    private readonly OsInfoService _osInfoService;
    private readonly ExpensiveProcessService _expensiveProcessService;
    private readonly VersionService _versionService;
    private readonly HostnameService _hostnameService;
    private readonly BootTimeService _bootTimeService;
    private readonly ServerAccess _serverAccess;
    private readonly ILogger<SubmitService> _logger;

    public SubmitService(
        MotdService motdService,
        ClientIdService clientIdService,
        SkuInfoService skuInfoService,
        OsInfoService osInfoService,
        ExpensiveProcessService expensiveProcessService,
        VersionService versionService,
        HostnameService hostnameService,
        BootTimeService bootTimeService,
        ServerAccess serverAccess,
        ILogger<SubmitService> logger)
    {
        _motdService = motdService;
        _clientIdService = clientIdService;
        _skuInfoService = skuInfoService;
        _osInfoService = osInfoService;
        _expensiveProcessService = expensiveProcessService;
        _versionService = versionService;
        _hostnameService = hostnameService;
        _bootTimeService = bootTimeService;
        _serverAccess = serverAccess;
        _logger = logger;
    }

    private async Task SubmitAsync(DstatResult[] statResults)
    {
        _logger.LogInformation("Gathering metrics...");

        var bootTime = await _bootTimeService.GetBootTimeAsync();
        _logger.LogTrace($"Boot time: {bootTime}.");

        var hostname = await _hostnameService.GetHostnameAsync();
        _logger.LogTrace($"Hostname: {hostname}.");

        var version = _versionService.GetAppVersion();
        _logger.LogTrace($"Version: {version}.");

        var expensiveProcess = await _expensiveProcessService.GetExpensiveProcessAsync();
        _logger.LogTrace($"Expensive process: {expensiveProcess}.");

        var osName = await _osInfoService.GetOsInfoAsync();
        _logger.LogTrace($"OS: {osName}.");
        
        var cpuCores = await _skuInfoService.GetCpuCores();
        _logger.LogTrace($"CPU cores: {cpuCores}.");
        
        var totalRam = await _skuInfoService.GetTotalRamInGb();
        _logger.LogTrace($"Total RAM: {totalRam}.");
        
        var (totalRoot, usedRoot) = await _skuInfoService.GetRootDriveSizeInGb();
        _logger.LogTrace($"Disk size: {usedRoot}/{totalRoot}.");
        
        var clientId = await _clientIdService.GetClientId();
        _logger.LogTrace($"Client id: {clientId}.");
        
        var motd = await _motdService.GetMotdFirstLine();
        _logger.LogTrace($"MOTD: {motd}.");

        _logger.LogTrace("Sending metrics...");
        try
        {
            var response =
                await _serverAccess.MetricsAsync(
                    clientId, 
                    hostname, 
                    bootTime, 
                    version, 
                    expensiveProcess, 
                    osName, 
                    cpuCores, 
                    totalRam, 
                    usedRoot, 
                    totalRoot,
                    motd,
                    statResults);
            _logger.LogInformation("Metrics sent! Response: {ResponseMessage}.", response.Message);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Failed to send metrics!");
            throw;
        }
    }

    public Task Consume(DstatResult[] items)
    {
        return SubmitAsync(items);
    }
}