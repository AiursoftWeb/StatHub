using Aiursoft.AiurObserver;
using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

public class SubmitService : IConsumer<DstatResult[]>
{
    private readonly ExpensiveProcessService _expensiveProcessService;
    private readonly VersionService _versionService;
    private readonly HostnameService _hostnameService;
    private readonly BootTimeService _bootTimeService;
    private readonly ServerAccess _serverAccess;
    private readonly ILogger<SubmitService> _logger;

    public SubmitService(
        ExpensiveProcessService expensiveProcessService,
        VersionService versionService,
        HostnameService hostnameService,
        BootTimeService bootTimeService,
        ServerAccess serverAccess,
        ILogger<SubmitService> logger)
    {
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
        _logger.LogInformation($"Boot time: {bootTime}.");
        
        var hostname = await _hostnameService.GetHostnameAsync();
        _logger.LogInformation($"Hostname: {hostname}.");
        
        var version = _versionService.GetAppVersion();
        _logger.LogInformation($"Version: {version}.");
        
        var expensiveProcess = await _expensiveProcessService.GetExpensiveProcessAsync();
        _logger.LogInformation($"Expensive process: {expensiveProcess}.");

        _logger.LogInformation("Sending metrics...");
        var response = await _serverAccess.MetricsAsync(hostname, bootTime, version, expensiveProcess, dstatResults);
        _logger.LogInformation("Metrics sent! Response: {ResponseMessage}.", response.Message);
    }

    public Func<DstatResult[], Task> Consume => SubmitAsync;
}