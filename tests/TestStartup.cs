// ReSharper disable all
using Aiursoft.StatHub.Services;
using Aiursoft.StatHub.Tests.MockServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace Aiursoft.StatHub.Tests;

public class TestStartup : Startup
{
    public override void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        base.ConfigureServices(configuration, environment, services);
        
        // Mocking services
        services.AddScoped<IpGeolocationService, MockIpGeolocationService>();
    }
}
