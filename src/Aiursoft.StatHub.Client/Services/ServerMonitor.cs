using System.Diagnostics.CodeAnalysis;
using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.Command;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

[ExcludeFromCodeCoverage] // This class is not a part of our test.
public class ServerMonitor(
    SubmitService submitService,
    LongCommandRunner commandService,
    ILogger<ServerMonitor> logger)
{
    private ISubscription? _subscription;

    public async Task MonitorServerAsync(CancellationToken cancellationToken, bool onlyOneTrigger = false)
    {
        try
        {
            logger.LogInformation("Starting to monitor server...");
            _subscription = commandService
                .Output
                .Filter(t => !string.IsNullOrWhiteSpace(t))
                .Filter(t => !t.StartsWith("----"))
                .Filter(t => !t.StartsWith("usr"))
                .Map(t => t.Replace("|", " "))
                .Filter(t => t.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length >= 16)
                .Pipe(t => { logger.LogTrace("Received dstat metrics: {Trace}.", t); })
                .Map(t => new DstatResult(t))
                .Aggregate(10) // Aggregate 10 results into one array. 
                .Throttle(TimeSpan.FromSeconds(9)) // Max speed is every 9 seconds one request.
                .InNewThread(e => { logger.LogCritical(e, "Failed to submit to server.");})
                .Pipe(result => { logger.LogTrace("Sending {Count} metrics.", result.Length); })
                .Subscribe(submitService);

            var args = "--cpu --mem --disk --net --load --nocolor --noheaders";
            if (onlyOneTrigger)
            {
                args += " 1 1";
            }

            await commandService.Run("dstat", args, Directory.GetCurrentDirectory());
        }
        finally
        {
            _subscription?.Unsubscribe();
        }
    }
}