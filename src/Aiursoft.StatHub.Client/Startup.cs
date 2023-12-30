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
        services.AddSingleton<IHostedService, ServerMonitor>();
        services.AddScoped<SubmitService>();
        services.AddTransient<UptimeService>();
        services.AddTransient<HostnameService>();
        services.AddTransient<CpuUsageService>();
        services.AddTransient<CommandService>();
    }
}