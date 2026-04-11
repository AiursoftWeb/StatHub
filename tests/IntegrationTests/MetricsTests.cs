using System.Net;
using System.Net.WebSockets;
using System.Text;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.StatHub.Data;
using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
        await _server.UpdateDbAsync<StatHubDbContext>();
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
                    memoryUsage = 1024L * 1024 * 10,
                    memoryLimit = 1024L * 1024 * 100,
                    uptime = "1 hour",
                    ports = "80/tcp",
                    isHealthy = true
                }
            }
        };

        var rpcMessage = new
        {
            jsonrpc = "2.0",
            method = "metrics",
            @params = metricsPayload
        };

        using (var ws = new ClientWebSocket())
        {
            await ws.ConnectAsync(new Uri($"ws://localhost:{_port}/api/agent/channel?clientId={clientId}"), CancellationToken.None);
            var json = JsonConvert.SerializeObject(rpcMessage);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            
            // Give the server some time to process the message
            await Task.Delay(500);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }

        // Now check if it's in the database
        var database = _server!.Services.GetRequiredService<InMemoryDatabase>();
        var agent = database.GetClient(clientId);
        Assert.IsNotNull(agent);
        Assert.AreEqual(kernelVersion, agent.KernelVersion);
        Assert.AreEqual(1, agent.Containers.Count);
        Assert.AreEqual("test-container", agent.Containers[0].Name);

        // Now check if it's displayed on the details page.
        await LoginAsAdminAsync();
        
        var detailsResponse = await _http.GetAsync($"/Dashboard/Details/{clientId}");
        detailsResponse.EnsureSuccessStatusCode();
        var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
        Assert.IsTrue(detailsHtml.Contains(kernelVersion));
        Assert.IsTrue(detailsHtml.Contains("test-container"));
        Assert.IsTrue(detailsHtml.Contains("test-image"));
        Assert.IsTrue(detailsHtml.Contains("data-sort=\"10485760\"")); // 10MB in bytes
    }

    [TestMethod]
    public async Task TestCommandExecution()
    {
        var clientId = Guid.NewGuid().ToString();
        var commandText = "echo 'hello world'";

        using (var ws = new ClientWebSocket())
        {
            await ws.ConnectAsync(new Uri($"ws://localhost:{_port}/api/agent/channel?clientId={clientId}"), CancellationToken.None);
            
            // Now as admin, send a command via HTTP
            await LoginAsAdminAsync();
            var token = await GetAntiCsrfToken($"/Dashboard/Details/{clientId}");
            var sendResponse = await _http.PostAsync($"/Dashboard/SendCommand/{clientId}", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "commandText", commandText },
                { "__RequestVerificationToken", token }
            }));
            Assert.AreEqual(HttpStatusCode.Found, sendResponse.StatusCode);

            // Agent side: Receive the command
            var buffer = new byte[1024 * 4];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var rpcRequest = JsonConvert.DeserializeObject<JsonRpcMessage>(messageJson);
            
            Assert.IsNotNull(rpcRequest);
            Assert.AreEqual("exec", rpcRequest.Method);
            var cmdId = rpcRequest.Id;
            var cmdParams = rpcRequest.Params?.ToObject<Dictionary<string, string>>();
            Assert.AreEqual(commandText, cmdParams?["cmd"]);

            // Agent side: Send back stdout chunk and done
            var stdoutMessage = new
            {
                jsonrpc = "2.0",
                method = "stdout-chunk",
                @params = new { id = cmdId, data = "hello world\n" }
            };
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(stdoutMessage))), WebSocketMessageType.Text, true, CancellationToken.None);

            var doneMessage = new
            {
                jsonrpc = "2.0",
                method = "exec-done",
                @params = new { id = cmdId, exitCode = 0 }
            };
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(doneMessage))), WebSocketMessageType.Text, true, CancellationToken.None);

            // Give the server some time to process
            await Task.Delay(500);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }

        // Now check if the result is in the database/history
        var database = _server!.Services.GetRequiredService<InMemoryDatabase>();
        var agent = database.GetClient(clientId);
        Assert.IsNotNull(agent);
        var exec = agent.CommandHistory.Values.FirstOrDefault();
        Assert.IsNotNull(exec);
        Assert.AreEqual(0, exec.ExitCode);
        Assert.AreEqual("hello world\n", exec.Stdout);
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
