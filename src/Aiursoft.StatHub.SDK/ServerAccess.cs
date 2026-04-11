using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Models;
using Aiursoft.AiurProtocol.Services;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aiursoft.StatHub.SDK;

public class ServerAccess(
    ILogger<ServerAccess> logger,
    AiurProtocolClient http,
    IOptions<ServerConfig> demoServerLocator)
{
    private readonly ServerConfig _serverLocator = demoServerLocator.Value;

    public Task<AiurResponse> InfoAsync()
    {
        var url = new AiurApiEndpoint(host: _serverLocator.Instance, route: "/api/info", param: new { });
        return http.Get<AiurResponse>(url);
    }
}
