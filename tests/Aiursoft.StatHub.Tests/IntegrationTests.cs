﻿using Aiursoft.AiurProtocol;
using Aiursoft.CommandFramework;
using Aiursoft.CSTools.Tools;
using Aiursoft.StatHub.Client;
using Aiursoft.StatHub.SDK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.StatHub.Tests;

[TestClass]
public class IntegrationTests
{
    private readonly SingleCommandApp<ClientHandler> _program = new();
    private readonly string _endpointUrl;
    private readonly int _port;
    private IHost? _server;

    public IntegrationTests()
    {
        _port = Network.GetAvailablePort();
        _endpointUrl = $"http://localhost:{_port}";
    }

    [TestInitialize]
    public Task CreateServer()
    {
        _server = App<Aiursoft.StatHub.Server.Startup>(Array.Empty<string>(), port: _port);
        return _server.StartAsync();
    }

    [TestMethod]
    public async Task TestApiCall()
    {
        var services = new ServiceCollection();
        services.AddStatHubServer(_endpointUrl);
        var serviceProvider = services.BuildServiceProvider();
        var sdk = serviceProvider.GetRequiredService<ServerAccess>();

        var result = await sdk.InfoAsync();
        Assert.AreEqual(result.Code, Code.ResultShown);
        Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Message));
    }

    [TestMethod]
    public async Task CommandTest()
    {
        var result = await _program.TestRunAsync(new[] { "-s", _endpointUrl, "--one-time" });
        Assert.AreEqual(0, result.ProgramReturn);
    }
    [TestMethod]
    public async Task BadCommandTest()
    {
        try
        {
            _ = await _program.TestRunAsync(new[] { "-s", "http://bad", "--one-time" });
            Assert.Fail();
        }
        catch
        {
            // Ignore
        }
    }
}
