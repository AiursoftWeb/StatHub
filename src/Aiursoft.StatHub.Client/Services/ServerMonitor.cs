using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.Command;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

    public Task MonitorServerAsync(CancellationToken cancellationToken, bool onlyOneTrigger = false)
    {
        _subscription = _commandService
            .Output
            .Filter(t => !string.IsNullOrWhiteSpace(t))
            .Filter(t => !t.StartsWith("----"))
            .Filter(t => !t.StartsWith("usr"))
            .Map(t => t.Replace("|", " "))
            .Filter(t => t.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length >= 16)
            .Map(t => new DstatResult(t))
            .Aggregate(10)
            .Throttle(TimeSpan.FromSeconds(9))
            .Pipe(result =>
            {
                _logger.LogInformation($"Sending metrics: {JsonConvert.SerializeObject(result)}.");
            })
            .Subscribe(_submitService);

        var args = "--cpu --mem --disk --net --load --nocolor  --noheaders";
        if (onlyOneTrigger)
        {
            args += " 1 1";
        }
        return _commandService.Run("dstat", args, Directory.GetCurrentDirectory());
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Unsubscribe();
        _commandService.Stop();
        return Task.CompletedTask;
    }
}