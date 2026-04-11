using Aiursoft.Canon;
using Aiursoft.StatHub.Services;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Tests.MockServices;

public class MockIpGeolocationService(
    CacheService cacheService,
    IHttpClientFactory httpClientFactory,
    ILogger<IpGeolocationService> logger)
    : IpGeolocationService(cacheService, httpClientFactory, logger)
{
    public new Task<(string CountryName, string CountryCode)?> GetLocationAsync(string ip)
    {
        if (ip == "1.1.1.1")
        {
            return Task.FromResult<(string CountryName, string CountryCode)?> (("Australia", "AU"));
        }
        return Task.FromResult<(string CountryName, string CountryCode)?> (("United States", "US"));
    }
}
