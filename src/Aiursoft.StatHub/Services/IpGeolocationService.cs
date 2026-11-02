using Aiursoft.Canon;
using Aiursoft.Scanner.Abstractions;
using Newtonsoft.Json;
using System.Net;

namespace Aiursoft.StatHub.Services;

public class IpGeolocationService(
    CacheService cacheService,
    IHttpClientFactory httpClientFactory,
    ILogger<IpGeolocationService> logger) : IScopedDependency
{
    private static readonly IPNetwork[] LocalNetworks =
    [
        IPNetwork.Parse("0.0.0.0/8"),
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("100.64.0.0/10"),
        IPNetwork.Parse("169.254.0.0/16"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("224.0.0.0/4"),
        IPNetwork.Parse("240.0.0.0/4"),
        IPNetwork.Parse("fc00::/7")
    ];

    public Task<(string CountryName, string CountryCode)?> GetLocationAsync(string ip)
    {
        if (!IPAddress.TryParse(ip, out var address))
            return Task.FromResult<(string CountryName, string CountryCode)?>(null);

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        // Local and multicast addresses have no public country to look up.
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.IPv6Any) ||
            address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast ||
            LocalNetworks.Any(network => network.Contains(address)))
            return Task.FromResult<(string CountryName, string CountryCode)?>(null);

        var publicIp = address.ToString();
        return cacheService.RunWithCache($"ip-location-info-{publicIp}", () => GetLocationFromServiceProviderAsync(publicIp));
    }

    private async Task<(string CountryName, string CountryCode)?> GetLocationFromServiceProviderAsync(string ip)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            // api.country.is is free and supports HTTPS.
            var response = await client.GetAsync($"https://api.country.is/{ip}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<IpApiResponse>(content);

                if (!string.IsNullOrWhiteSpace(result?.Country))
                {
                    try
                    {
                        var region = new System.Globalization.RegionInfo(result.Country);
                        return (region.EnglishName, result.Country);
                    }
                    catch
                    {
                        // In case the country code is invalid or not found
                        return (result.Country, result.Country);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get location for IP: {IP}", ip);
        }

        return null;
    }
}
