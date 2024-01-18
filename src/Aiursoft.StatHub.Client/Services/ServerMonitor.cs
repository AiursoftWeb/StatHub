using System.Diagnostics.CodeAnalysis;
using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.Command;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

[ExcludeFromCodeCoverage] // This class is not a part of our test.
public class ServerMonitor
{
    private readonly ILogger<ServerMonitor> _logger;
    private readonly SubmitService _submitService;
    private readonly LongCommandRunner _commandService;
    private ISubscription? _subscription;

    public ServerMonitor(
        SubmitService submitService,
        LongCommandRunner commandService,
        ILogger<ServerMonitor> logger)
    {
        _submitService = submitService;
        _commandService = commandService;
        _logger = logger;
    }

    public async Task MonitorServerAsync(CancellationToken cancellationToken, bool onlyOneTrigger = false)
    {
        try
        {
            _logger.LogInformation("Starting to monitor server...");
            _subscription = _commandService
                .Output
                .Filter(t => !string.IsNullOrWhiteSpace(t))
                .Filter(t => !t.StartsWith("----"))
                .Filter(t => !t.StartsWith("usr"))
                .Map(t => t.Replace("|", " "))
                .Filter(t => t.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length >= 16)
                .Pipe(t => { _logger.LogTrace("Received dstat metrics: {Trace}.", t); })
                .Map(t => new DstatResult(t))
                .Aggregate(10) // Aggregate 10 results into one array. 
                .Throttle(TimeSpan.FromSeconds(9)) // Max speed is every 9 seconds one request.
                .Pipe(result => { _logger.LogTrace("Sending {Count} metrics.", result.Length); })
                .Subscribe(_submitService);

            var args = "--cpu --mem --disk --net --load --nocolor --noheaders";
            if (onlyOneTrigger)
            {
                args += " 1 1";
            }

            await _commandService.Run("dstat", args, Directory.GetCurrentDirectory());
        }
        finally
        {
            _subscription?.Unsubscribe();
        }
    }
}