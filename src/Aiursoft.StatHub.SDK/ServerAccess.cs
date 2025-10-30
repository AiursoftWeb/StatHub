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

    public Task<AiurResponse> MetricsAsync(
        string clientId,
        string hostname,
        DateTime bootTime,
        string version,
        string process,
        string osName,
        int cpuCores,
        int ramInGb,
        int usedRoot,
        int totalRoot,
        string? motd,
        DstatResult[] stats)
    {
        var url = new AiurApiEndpoint(_serverLocator.Instance, "/api/metrics", new { });
        var form = new AiurApiPayload(new MetricsAddressModel
        {
            ClientId = clientId,
            Hostname = hostname,
            BootTime = bootTime,
            Version = version,
            Process = process,
            OsName = osName,
            CpuCores = cpuCores,
            RamInGb = ramInGb,
            UsedRoot = usedRoot,
            TotalRoot = totalRoot,
            Motd = motd,
            Stats = stats
        });
        logger.LogInformation("Sending metrics to endpoint: '{Endpoint}'", url);
        return http.Post<AiurResponse>(url, form, BodyFormat.HttpJsonBody, autoRetry: false,
            enforceSameVersion: false);
    }
}
