using Aiursoft.StatHub.SDK;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client;

public class SubmitService
{
    private readonly ServerAccess _serverAccess;
    private readonly ILogger<SubmitService> _logger;

    public SubmitService(
        ServerAccess serverAccess,
        ILogger<SubmitService> logger)
    {
        _serverAccess = serverAccess;
        _logger = logger;
    }
    
    public async Task SubmitAsync()
    {
        var info = await _serverAccess.InfoAsync();
        _logger.LogInformation(info.Message);
    }
}