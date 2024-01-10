using Aiursoft.AiurProtocol;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.StatHub.SDK;

public static class Extensions
{
    public static IServiceCollection AddStatHubServer(this IServiceCollection services, string endPointUrl)
    {
        services.AddAiurProtocolClient();
        services.Configure<ServerConfig>(options => options.Instance = endPointUrl);
        services.AddScoped<ServerAccess>();
        return services;
    }

    public static Version GetSdkVersion()
    {
        return typeof(Extensions).Assembly.GetName().Version!;
    }
}