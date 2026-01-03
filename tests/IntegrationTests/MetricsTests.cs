using System.Net;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.StatHub.Data;
using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.SDK.Models;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public class MetricsTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public MetricsTests()
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = false
        };
        _port = Network.GetAvailablePort();
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://localhost:{_port}")
        };
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = await AppAsync<Startup>([], port: _port);
        await _server.UpdateDbAsync<TemplateDbContext>();
        await _server.SeedAsync();
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server == null) return;
        await _server.StopAsync();
        _server.Dispose();
    }

    [TestMethod]
    public async Task TestSubmitMetricsAndDisplay()
    {
        var clientId = Guid.NewGuid().ToString();
        var hostname = "test-host";
        var kernelVersion = "6.8.0-test-kernel";
        var osName = "Test OS";
        
        // Let's just use HttpClient to post directly to the API to be sure.
        var metricsPayload = new
        {
            clientId,
            hostname,
            bootTime = DateTime.UtcNow.AddDays(-1),
            version = "1.0.0",
            process = "test-process",
            osName,
            kernelVersion,
            cpuCores = 4,
            ramInGb = 8,
            usedRoot = 20,
            totalRoot = 100,
            motd = "Test MOTD",
            stats = Enumerable.Range(0, 10).Select(_ => new DstatResult("0 0 100 0 0 100M 700M 0 0 0 0 0 0 0.0 0.0 0.0")).ToArray(),
            containers = new[]
            {
                new 
                {
                    id = "container-id-1234567890",
                    name = "test-container",
                    image = "test-image",
                    state = "running",
                    status = "Up 1 hour",
                    cpuPercentage = 1.5,
                    memoryUsage = 1024 * 1024 * 10,
                    memoryLimit = 1024 * 1024 * 100,
                    uptime = "1 hour",
                    ports = "80/tcp",
                    isHealthy = true
                }
            }
        };

        var response = await _http.PostAsJsonAsync("/api/metrics", metricsPayload);
        response.EnsureSuccessStatusCode();

        // Now check if it's in the database
        var database = _server!.Services.GetRequiredService<InMemoryDatabase>();
        var agent = database.GetClient(clientId);
        Assert.IsNotNull(agent);
        Assert.AreEqual(kernelVersion, agent.KernelVersion);
        Assert.HasCount(1, agent.Containers);
        Assert.AreEqual("test-container", agent.Containers[0].Name);

        // Now check if it's displayed on the details page.
        // We need to login first to see the dashboard.
        await LoginAsAdminAsync();
        
        var detailsResponse = await _http.GetAsync($"/Dashboard/Details/{clientId}");
        detailsResponse.EnsureSuccessStatusCode();
        var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains(kernelVersion, detailsHtml);
        Assert.Contains("test-container", detailsHtml);
        Assert.Contains("test-image", detailsHtml);
    }

    private async Task LoginAsAdminAsync()
    {
        var email = "admin@default.com";
        var password = "admin123";

        var loginToken = await GetAntiCsrfToken("/Account/Login");
        var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "EmailOrUserName", email },
            { "Password", password },
            { "__RequestVerificationToken", loginToken }
        });
        var response = await _http.PostAsync("/Account/Login", loginContent);
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
    }

    private async Task<string> GetAntiCsrfToken(string url)
    {
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />");
        return match.Groups[1].Value;
    }
}
