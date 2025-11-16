using System.CommandLine;
using Aiursoft.CommandFramework.Framework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.CommandFramework.Services;
using Aiursoft.StatHub.Client.Services;
using Aiursoft.StatHub.SDK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client;

public class ClientHandler : ExecutableCommandHandlerBuilder
{
    public static readonly Option<string> Server =
        new(
            name: "--server",
            aliases: ["-s"])
        {
            Description = "The target server to use.",
            Required = true
        };

    private static readonly Option<bool> OneTime =
        new(
            name: "--one-time",
            aliases: ["-o"])
        {
            Description = "Only connect to the server once.",
            DefaultValueFactory = _ => false
        };

    protected override string Name => "client";

    protected override string Description => "Serve as a client to connect to a StatHub server.";

    protected override Option[] GetCommandOptions() =>
    [
        Server,
        OneTime,
        CommonOptionsProvider.VerboseOption
    ];

    protected override async Task Execute(ParseResult context)
    {
        var server = context.GetValue(Server)!;
        var oneTime = context.GetValue(OneTime);
        var verbose = context.GetValue(CommonOptionsProvider.VerboseOption);
        var hostBuilder = ServiceBuilder
            .CreateCommandHostBuilder<Startup>(verbose);

        hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddStatHubServer(server);
        });

        var host = hostBuilder.Build();
        var serverMonitor = host.Services.GetRequiredService<ServerMonitor>();
        var logger = host.Services.GetRequiredService<ILogger<ServerMonitor>>();

        try
        {
            await serverMonitor.MonitorServerAsync(CancellationToken.None, oneTime);
        }
        catch (InvalidOperationException) when (oneTime)
        {
            // Ignore because dstat will quit immediately in one-time mode.
        }

        logger.LogInformation("The monitor quit.");
    }
}
