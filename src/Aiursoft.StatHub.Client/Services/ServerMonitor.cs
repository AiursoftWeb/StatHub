﻿using System.Diagnostics.CodeAnalysis;
using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.Command;
using Aiursoft.StatHub.SDK.Models;
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
            _logger.LogInformation("Starting to monitor server...");
            _subscription = _commandService
                .Output
                .Pipe(t => { _logger.LogTrace("Received metrics: {Trace}.", t); })
                .Filter(t => !string.IsNullOrWhiteSpace(t))
                .Filter(t => !t.StartsWith("----"))
                .Filter(t => !t.StartsWith("usr"))
                .Map(t => t.Replace("|", " "))
                .Filter(t => t.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length >= 16)
                .Map(t => new DstatResult(t))
                .Aggregate(10) // Aggregate 10 results into one array. 
                .Throttle(TimeSpan.FromSeconds(9)) // Max speed is every 9 seconds one request.
                .Pipe(result => { _logger.LogTrace("Sending metrics: {Trace}.", JsonConvert.SerializeObject(result)); })
                .Subscribe(_submitService);

            var args = "--cpu --mem --disk --net --load --nocolor --noheaders";
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