using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.StatHub.Services.Monitoring;

namespace Aiursoft.StatHub.Services.BackgroundJobs;

public sealed class MonitoringJob(MonitoringService monitoring) : IBackgroundJob
{
    public string Name => "AlertCenter monitoring";
    public string Description => "Evaluates sustained server health rules and synchronizes incidents with AlertCenter.";
    public Task ExecuteAsync() => monitoring.CheckAsync();
}
