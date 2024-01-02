using Aiursoft.Canon;
using Aiursoft.CommandFramework.Abstracts;
using Aiursoft.CSTools.Services;
using Aiursoft.StatHub.Client.Services;
using Aiursoft.StatHub.Client.Services.Stat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aiursoft.StatHub.Client;

public class Startup : IStartUp
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTaskCanon();
        services.AddSingleton<IHostedService, ServerMonitor>();
        services.AddSingleton<DstatMonitor>();
        services.AddScoped<SubmitService>();
        services.AddTransient<BootTimeService>();
        services.AddTransient<HostnameService>();
        services.AddTransient<CpuUsageService>();
        services.AddTransient<VersionService>();
        services.AddTransient<ExpensiveProcessService>();
        services.AddTransient<CommandService>();
    }
}