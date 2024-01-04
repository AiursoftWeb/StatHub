using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Aiursoft.AiurObserver;
using Aiursoft.CommandFramework.Framework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.CommandFramework.Services;
using Aiursoft.StatHub.Client.Services;
using Aiursoft.StatHub.SDK;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Aiursoft.StatHub.Client;

public class ClientHandler : ExecutableCommandHandlerBuilder
{
    public static readonly Option<string> Server =
        new(
            aliases: new[] { "-s", "--server" },
            description: "The target server to use.")
        {
            IsRequired = true
        };

    private static readonly Option<bool> OneTime =
        new(
            aliases: new[] { "-o", "--one-time" },
            description: "Only connect to the server once.",
            getDefaultValue: () => false);

    protected override string Name => "client";

    protected override string Description => "Serve as a client to connect to a StatHub server.";
    
    protected override Option[] GetCommandOptions() => new Option[]
    {
        Server,
        OneTime,
        CommonOptionsProvider.VerboseOption
    };

    protected override async Task Execute(InvocationContext context)
    {
        var server = context.ParseResult.GetValueForOption(Server)!;
        var oneTime = context.ParseResult.GetValueForOption(OneTime);
        var verbose = context.ParseResult.GetValueForOption(CommonOptionsProvider.VerboseOption);
        var hostBuilder = ServiceBuilder
            .CreateCommandHostBuilder<Startup>(verbose);

        hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddStatHubServer(server);
        });
        
        var host = hostBuilder.Build();
        var serverMonitor = host.Services.GetRequiredService<ServerMonitor>();

        if (oneTime)
        {
            try
            {
                await serverMonitor.MonitorServerAsync(CancellationToken.None, true);
            }
            catch (InvalidOperationException)
            {
                // Ignore because dstat will quit immediately.
            }
        }
        else
        {
            await serverMonitor.MonitorServerAsync(CancellationToken.None);
        }
    }
}