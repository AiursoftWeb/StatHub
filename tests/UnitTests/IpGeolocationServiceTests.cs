using System.Net;
using Aiursoft.Canon;
using Aiursoft.StatHub.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aiursoft.StatHub.Tests.UnitTests;

[TestClass]
public class IpGeolocationServiceTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("not-an-ip")]
    [DataRow("127.0.0.1")]
    [DataRow("0.0.0.0")]
    [DataRow("10.0.0.1")]
    [DataRow("100.64.0.1")]
    [DataRow("100.127.255.255")]
    [DataRow("169.254.1.1")]
    [DataRow("172.16.0.1")]
    [DataRow("172.20.0.1")]
    [DataRow("172.31.255.255")]
    [DataRow("192.168.1.1")]
    [DataRow("224.0.0.1")]
    [DataRow("255.255.255.255")]
    [DataRow("::")]
    [DataRow("::1")]
    [DataRow("fe80::1")]
    [DataRow("fec0::1")]
    [DataRow("fc00::1")]
    [DataRow("fdff::1")]
    [DataRow("ff02::1")]
    [DataRow("::ffff:172.20.0.1")]
    [DataRow("::ffff:127.0.0.1")]
    public async Task LocalAddressesNeverContactProvider(string? ip)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var handler = new LocationHandler();
        using var client = new HttpClient(handler);
        var service = CreateService(cache, client);

        for (var attempt = 0; attempt < 3; attempt++)
            Assert.IsNull(await service.GetLocationAsync(ip!));

        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    [DataRow("8.8.8.8", "8.8.8.8")]
    [DataRow("172.15.255.255", "172.15.255.255")]
    [DataRow("172.32.0.1", "172.32.0.1")]
    [DataRow("100.128.0.1", "100.128.0.1")]
    [DataRow("192.169.1.1", "192.169.1.1")]
    [DataRow("2001:4860:4860::8888", "2001:4860:4860::8888")]
    [DataRow("::ffff:8.8.8.8", "8.8.8.8")]
    public async Task PublicAddressesRetainLookupAndCache(string ip, string expectedIp)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var handler = new LocationHandler();
        using var client = new HttpClient(handler);
        var service = CreateService(cache, client);

        Assert.AreEqual("US", (await service.GetLocationAsync(ip))?.CountryCode);
        Assert.AreEqual("US", (await service.GetLocationAsync(ip))?.CountryCode);
        Assert.HasCount(1, handler.Requests);
        Assert.AreEqual($"https://api.country.is/{expectedIp}", handler.Requests[0].ToString());
    }

    private static IpGeolocationService CreateService(IMemoryCache cache, HttpClient client) =>
        new(new CacheService(cache, NullLogger<CacheService>.Instance),
            new LocationClientFactory(client), NullLogger<IpGeolocationService>.Instance);

    private sealed class LocationClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class LocationHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"country\":\"US\"}")
            });
        }
    }
}
