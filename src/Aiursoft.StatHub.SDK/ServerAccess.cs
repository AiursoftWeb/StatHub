using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Attributes;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.SDK.Models;
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
    
    public Task<AiurCollection<Client>> GetClientsAsync()
    {
        var url = new AiurApiEndpoint(host: _serverLocator.Instance, route: "/api/clients", param: new {});
        return _http.Get<AiurCollection<Client>>(url);
    }

    public Task<AiurResponse> InfoAsync()
    {
        var url = new AiurApiEndpoint(host: _serverLocator.Instance, route: "/api/info", param: new {});
        return _http.Get<AiurResponse>(url);
    }
    
    public Task<AiurResponse> MetricsAsync(string hostname, DateTime bootTime, string version, string process, string osName, int cpuCores, int ramInGb, int usedRoot, int totalRoot, DstatResult[] stats)
    {
        var url = new AiurApiEndpoint(_serverLocator.Instance, "/api/metrics", new { });
        var form = new AiurApiPayload(new MetricsAddressModel
        {
            Hostname = hostname,
            BootTime = bootTime,
            Version = version,
            Process = process,
            OsName = osName,
            CpuCores = cpuCores,
            RamInGb = ramInGb,
            UsedRoot = usedRoot,
            TotalRoot = totalRoot,
            Stats = stats
        });
        return _http.Post<AiurResponse>(url, form, BodyFormat.HttpJsonBody, autoRetry: false,
            enforceSameVersion: false);
    }
}