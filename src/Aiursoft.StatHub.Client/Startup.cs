using Aiursoft.CommandFramework.Abstracts;
using Aiursoft.StatHub.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Startup : IStartUp
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IHostedService, ServerMonitor>();
        services.AddScoped<SubmitService>();
    }
}