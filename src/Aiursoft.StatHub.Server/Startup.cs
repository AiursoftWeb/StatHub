using System.Reflection;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.StatHub.Server.Data;
using Aiursoft.WebTools.Abstractions.Models;

namespace Aiursoft.StatHub.Server;

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services.AddSingleton<InMemoryDatabase>();

        services
            .AddControllersWithViews()
            .AddAiurProtocol()
            .AddApplicationPart(Assembly.GetExecutingAssembly());
    }

    public void Configure(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseRouting();
        app.MapDefaultControllerRoute();
    }
}