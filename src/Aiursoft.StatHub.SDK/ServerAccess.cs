using Aiursoft.AiurProtocol;
using Microsoft.Extensions.Options;

namespace Aiursoft.StatHub.SDK;

public class ServerAccess
{
    private readonly AiurProtocolClient _http;
    private readonly ServerConfig _serverLocator;

    public ServerAccess(
        AiurProtocolClient http,
        IOptions<ServerConfig> demoServerLocator)
    {
        _http = http;
        _serverLocator = demoServerLocator.Value;
    }

    public async Task<AiurResponse> InfoAsync()
    {
        var url = new AiurApiEndpoint(host: _serverLocator.Instance, route: "/api/info", param: new {});
        var result = await _http.Get<AiurResponse>(url);
        return result;
    }
}