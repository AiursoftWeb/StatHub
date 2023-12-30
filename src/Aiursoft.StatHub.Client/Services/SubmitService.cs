using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

public class SubmitService
{
    private readonly UptimeService _uptimeService;
    private readonly ServerAccess _serverAccess;
    private readonly ILogger<SubmitService> _logger;

    public SubmitService(
        UptimeService uptimeService,
        ServerAccess serverAccess,
        ILogger<SubmitService> logger)
    {
        _uptimeService = uptimeService;
        _serverAccess = serverAccess;
        _logger = logger;
    }
    
    public async Task SubmitAsync()
    {
        _logger.LogInformation("Submitting metrics...");
        var upTime = await _uptimeService.GetUpTimeAsync();
        await _serverAccess.MetricsAsync(upTime);
    }
}