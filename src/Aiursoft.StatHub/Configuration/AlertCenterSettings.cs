using Aiursoft.StatHub.Services;

namespace Aiursoft.StatHub.Configuration;

public interface IAlertCenterSettingsProvider
{
    Task<AlertCenterSettings> GetAsync();
}

public sealed class AlertCenterSettingsProvider(GlobalSettingsService settings) : IAlertCenterSettingsProvider
{
    private Task<AlertCenterSettings>? _snapshot;

    public Task<AlertCenterSettings> GetAsync() => _snapshot ??= LoadAsync();

    private async Task<AlertCenterSettings> LoadAsync() => new()
    {
        Enabled = await settings.GetBoolSettingAsync(SettingsMap.AlertCenterEnabled),
        Endpoint = await settings.GetSettingValueAsync(SettingsMap.AlertCenterEndpoint),
        ApiKey = await settings.GetSettingValueAsync(SettingsMap.AlertCenterApiKey),
        PublicBaseUrl = await settings.GetSettingValueAsync(SettingsMap.AlertCenterPublicBaseUrl)
    };
}

public sealed class AlertCenterSettings
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "https://alertcenter.aiursoft.com";
    public string ApiKey { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = "https://stathub.aiursoft.com";

    public void Validate()
    {
        ValidateUrl(Endpoint);
        ValidateUrl(PublicBaseUrl);
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("An AlertCenter application API key is required.");
    }

    private static void ValidateUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("AlertCenter integration requires HTTP(S) base URLs without credentials, query or fragment.");
    }
}
