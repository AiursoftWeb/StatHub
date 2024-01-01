﻿using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Attributes;
using Aiursoft.StatHub.SDK.AddressModels;
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

    public Task<AiurResponse> InfoAsync()
    {
        var url = new AiurApiEndpoint(host: _serverLocator.Instance, route: "/api/info", param: new {});
        return _http.Get<AiurResponse>(url);
    }
    
    public Task<AiurResponse> MetricsAsync(string hostname, int upTime, int cpuUsage)
    {
        var url = new AiurApiEndpoint(_serverLocator.Instance, "/api/metrics", new { });
        var form = new AiurApiPayload(new MetricsAddressModel
        {
            Hostname = hostname,
            UpTime = upTime,
            CpuUsage = cpuUsage
        });
        return _http.Post<AiurResponse>(url, form, BodyFormat.HttpJsonBody);
    }
}