using Aiursoft.AiurProtocol.Services;
using Aiursoft.StatHub.Configuration;
using Microsoft.Extensions.Options;
using Aiursoft.AlertCenter.SDK;
using Aiursoft.AlertCenter.SDK.Models;

namespace Aiursoft.StatHub.Services.Monitoring;

public interface IMonitoringIncidentClient
{
    Task<QueryIncidentsResponse> QueryAsync(QueryIncidentsRequest request);
    Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request);
    Task<ChangeIncidentSeverityResponse> ChangeSeverityAsync(Guid incidentId, ChangeIncidentSeverityRequest request);
    Task<ResolveIncidentResponse> ResolveAsync(Guid incidentId, ResolveIncidentRequest request);
}

public sealed class MonitoringIncidentClient(AiurProtocolClient http, IAlertCenterSettingsProvider settingsProvider) : IMonitoringIncidentClient
{
    private Task<AlertCenterApiClient>? _client;

    private async Task<AlertCenterApiClient> CreateClientAsync()
    {
        var settings = await settingsProvider.GetAsync();
        settings.Validate();
        return new AlertCenterApiClient(http, Options.Create(new AlertCenterApiOptions
        {
            Endpoint = settings.Endpoint.TrimEnd('/'), ApiKey = settings.ApiKey
        }), new SnapshotKeyProvider(settings.ApiKey));
    }

    private Task<AlertCenterApiClient> GetClientAsync() => _client ??= CreateClientAsync();

    public async Task<QueryIncidentsResponse> QueryAsync(QueryIncidentsRequest request) =>
        await (await GetClientAsync()).QueryIncidentsAsync(request);
    public async Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request) =>
        await (await GetClientAsync()).CreateIncidentAsync(request);
    public async Task<ResolveIncidentResponse> ResolveAsync(Guid incidentId, ResolveIncidentRequest request) =>
        await (await GetClientAsync()).ResolveIncidentAsync(incidentId, request);

    public async Task<ChangeIncidentSeverityResponse> ChangeSeverityAsync(Guid incidentId, ChangeIncidentSeverityRequest request) =>
        await (await GetClientAsync()).ChangeIncidentSeverityAsync(incidentId, request);

    private sealed class SnapshotKeyProvider(string key) : IAlertCenterApiKeyProvider
    {
        public ValueTask<string> GetApiKeyAsync() => ValueTask.FromResult(key);
    }
}
