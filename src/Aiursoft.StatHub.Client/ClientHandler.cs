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
        var stopWatch = new Stopwatch();
        
        var dstatMonitor = host.Services.GetRequiredService<DstatMonitor>();
        dstatMonitor
            .Filter(t => !string.IsNullOrWhiteSpace(t))
            .Filter(t => !t.StartsWith("----"))
            .Filter(t => !t.StartsWith("usr"))
            .Map(t => t.Replace("|", " "))
            .Filter(t => t.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length >= 16)
            .Map(t => new DstatResult(t))
            .Aggregate(10)
            .Map(JsonConvert.SerializeObject)
            .Subscribe(result =>
        {
            Console.WriteLine($"Time: {stopWatch.ElapsedMilliseconds}ms");
            Console.WriteLine(result);
            return Task.CompletedTask;
        });

        stopWatch.Start();
        await dstatMonitor.Monitor();
        // if (oneTime)
        // {
        //     var submitService = host.Services.CreateScope().ServiceProvider.GetRequiredService<SubmitService>();
        //     await submitService.SubmitAsync();
        // }
        // else
        // {
        //     await host.StartAsync();
        //     await host.WaitForShutdownAsync();
        // }
    }
}