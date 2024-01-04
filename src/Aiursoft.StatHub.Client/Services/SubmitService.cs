using Aiursoft.AiurObserver;
using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

public class SubmitService : IConsumer<DstatResult[]>
{
    private readonly OsInfoService _osInfoService;
    private readonly ExpensiveProcessService _expensiveProcessService;
    private readonly VersionService _versionService;
    private readonly HostnameService _hostnameService;
    private readonly BootTimeService _bootTimeService;
    private readonly ServerAccess _serverAccess;
    private readonly ILogger<SubmitService> _logger;

    public SubmitService(
        OsInfoService osInfoService,
        ExpensiveProcessService expensiveProcessService,
        VersionService versionService,
        HostnameService hostnameService,
        BootTimeService bootTimeService,
        ServerAccess serverAccess,
        ILogger<SubmitService> logger)
    {
        _osInfoService = osInfoService;
        _expensiveProcessService = expensiveProcessService;
        _versionService = versionService;
        _hostnameService = hostnameService;
        _bootTimeService = bootTimeService;
        _serverAccess = serverAccess;
        _logger = logger;
    }
    
    public async Task SubmitAsync(DstatResult[] dstatResults)
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

        _logger.LogTrace("Sending metrics...");
        try
        {
            var response = await _serverAccess.MetricsAsync(hostname, bootTime, version, expensiveProcess, osName, dstatResults);
            _logger.LogInformation("Metrics sent! Response: {ResponseMessage}.", response.Message);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Failed to send metrics!");
            throw;
        }
    }

    public Func<DstatResult[], Task> Consume => SubmitAsync;
}