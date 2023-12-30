using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

[ExcludeFromCodeCoverage] // This class is not a part of our test.
public class ServerMonitor : IHostedService
{
    private Timer? _timer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ServerMonitor> _logger;

    public ServerMonitor(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ServerMonitor> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Timed Background Service is starting");
        _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Timed Background Service is stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var submitService = scope.ServiceProvider.GetRequiredService<SubmitService>();
        await submitService.SubmitAsync();
    }
}