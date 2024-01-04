using Aiursoft.AiurObserver.Command;
using Aiursoft.Canon;
using Aiursoft.CommandFramework.Abstracts;
using Aiursoft.CSTools.Services;
using Aiursoft.StatHub.Client.Services;
using Aiursoft.StatHub.Client.Services.Stat;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.StatHub.Client;

public class Startup : IStartUp
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTaskCanon();
        services.AddSingleton<ServerMonitor>();
        services.AddSingleton<LongCommandRunner>();
        services.AddSingleton<SubmitService>();
        services.AddSingleton<BootTimeService>();
        services.AddSingleton<HostnameService>();
        services.AddSingleton<OsInfoService>();
        services.AddSingleton<VersionService>();
        services.AddSingleton<ExpensiveProcessService>();
        services.AddSingleton<CommandService>();
    }
}