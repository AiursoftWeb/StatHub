using System.Reflection;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.WebTools.Models;

namespace Aiursoft.StatHub.Server;

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services
            .AddControllers()
            .AddAiurProtocol()
            .AddApplicationPart(Assembly.GetExecutingAssembly());
    }

    public void Configure(WebApplication app)
    {
        app.UseRouting();
        app.MapDefaultControllerRoute();
    }
}