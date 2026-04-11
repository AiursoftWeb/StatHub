// ReSharper disable all
using Microsoft.Extensions.Logging;
using Aiursoft.StatHub.Services;

namespace Aiursoft.StatHub.Tests.MockServices;

public class MockIpGeolocationService(
    Aiursoft.Canon.CacheService cacheService,
    System.Net.Http.IHttpClientFactory httpClientFactory,
    ILogger<IpGeolocationService> logger)
    : IpGeolocationService(cacheService, httpClientFactory, logger)
{
    public new System.Threading.Tasks.Task<(string CountryName, string CountryCode)?> GetLocationAsync(string ip)
    {
        if (ip == "1.1.1.1")
        {
            return System.Threading.Tasks.Task.FromResult<(string CountryName, string CountryCode)?> (("Australia", "AU"));
        }
        return System.Threading.Tasks.Task.FromResult<(string CountryName, string CountryCode)?> (("United States", "US"));
    }
}
