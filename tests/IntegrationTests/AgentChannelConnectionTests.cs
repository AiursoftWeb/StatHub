using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.WebSocket;
using Aiursoft.AiurObserver.WebSocket.Server;
using Aiursoft.StatHub.Client.Services;
using Aiursoft.StatHub.SDK;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public class AgentChannelConnectionTests
{
    [TestMethod]
    public async Task LongCommandDoesNotBlockAcknowledgementsOrReplies()
    {
        var commandStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCommand = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var replied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = await StartServer(async (socket, request) =>
        {
            if (request.Method == "metrics")
            {
                await socket.Send(JsonConvert.SerializeObject(new JsonRpcMessage { Method = "exec", Id = "command" }));
                await commandStarted.Task;
                await socket.Send(JsonConvert.SerializeObject(new JsonRpcMessage { Method = "metrics-ack", Id = "unrelated" }));
                await socket.Send(JsonConvert.SerializeObject(new JsonRpcMessage { Method = "metrics-ack", Id = request.Id }));
            }
            else if (request.Method == "exec-done") replied.TrySetResult();
        });
        var connection = Connection(server);
        using var stop = new CancellationTokenSource();
        var run = connection.RunAsync("server", async (command, reply) =>
        {
            commandStarted.TrySetResult();
            await releaseCommand.Task;
            await reply(new JsonRpcMessage { Method = "exec-done", Id = command.Id });
        }, stop.Token);
        try
        {
            await WaitConnected(connection);
            Assert.IsTrue(await connection.SendMetricsAsync(Metrics()).WaitAsync(TimeSpan.FromSeconds(5)));
            releaseCommand.TrySetResult();
            await replied.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            releaseCommand.TrySetResult();
            await stop.CancelAsync();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MissingOrWrongAcknowledgementClosesOldConnectionAndReconnects(bool wrongAcknowledgement)
    {
        var connections = 0;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = await StartServer(async (socket, request) =>
        {
            if (request.Method != "metrics") return;
            if (Volatile.Read(ref connections) == 1)
            {
                if (wrongAcknowledgement)
                    await socket.Send(JsonConvert.SerializeObject(new JsonRpcMessage { Method = "metrics-ack", Id = "stale-id" }));
                received.TrySetResult();
                return;
            }
            await socket.Send(JsonConvert.SerializeObject(new JsonRpcMessage { Method = "metrics-ack", Id = request.Id }));
        }, () =>
        {
            if (Interlocked.Increment(ref connections) > 1) reconnected.TrySetResult();
        });
        var connection = Connection(server);
        using var stop = new CancellationTokenSource();
        var run = connection.RunAsync("server", (_, _) => Task.CompletedTask, stop.Token);
        try
        {
            await WaitConnected(connection);
            var first = connection.SendMetricsAsync(Metrics());
            await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsFalse(await connection.SendMetricsAsync(Metrics()), "Keep at most one report awaiting acknowledgement.");
            Assert.IsFalse(await first.WaitAsync(TimeSpan.FromSeconds(35)), "A local send or a stale acknowledgement is not delivery.");
            await reconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await WaitConnected(connection);
            Assert.IsTrue(await connection.SendMetricsAsync(Metrics()).WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            await stop.CancelAsync();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [TestMethod]
    public async Task PeerClosureReleasesPendingReportAndReconnectsWithoutWaitingForAckTimeout()
    {
        var first = true;
        await using var server = await StartServer(async (socket, request) =>
        {
            if (request.Method != "metrics") return;
            if (first)
            {
                first = false;
                await socket.Close();
            }
            else await socket.Send(JsonConvert.SerializeObject(new JsonRpcMessage { Method = "metrics-ack", Id = request.Id }));
        });
        var connection = Connection(server);
        using var stop = new CancellationTokenSource();
        var run = connection.RunAsync("server", (_, _) => Task.CompletedTask, stop.Token);
        try
        {
            await WaitConnected(connection);
            Assert.IsFalse(await connection.SendMetricsAsync(Metrics()).WaitAsync(TimeSpan.FromSeconds(5)));
            await WaitConnected(connection);
            Assert.IsTrue(await connection.SendMetricsAsync(Metrics()).WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            await stop.CancelAsync();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [TestMethod]
    public async Task CancellationReleasesPendingReportAndStopsReconnectLoop()
    {
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = await StartServer((_, _) => { received.TrySetResult(); return Task.CompletedTask; });
        var connection = Connection(server);
        using var stop = new CancellationTokenSource();
        var run = connection.RunAsync("server", (_, _) => Task.CompletedTask, stop.Token);
        await WaitConnected(connection);
        var report = connection.SendMetricsAsync(Metrics());
        await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await stop.CancelAsync();
        Assert.IsFalse(await report.WaitAsync(TimeSpan.FromSeconds(5)));
        await run.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsFalse(connection.IsConnected);
    }

    private static MetricsAddressModel Metrics() => new() { Hostname = "test-server", CpuCores = 1, Stats = [] };

    private static AgentChannelConnection Connection(WebApplication server) => new(
        Options.Create(new ServerConfig { Instance = server.Urls.Single().Replace("0.0.0.0", "127.0.0.1") }), NullLogger<AgentChannelConnection>.Instance);

    private static async Task WaitConnected(AgentChannelConnection connection)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!connection.IsConnected) await Task.Delay(10, timeout.Token);
    }

    private static async Task<WebApplication> StartServer(Func<ObservableWebSocket, JsonRpcMessage, Task> receive, Action? connected = null)
    {
        using var reservation = new TestPortReservation();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{reservation.Port}");
        var app = builder.Build();
        reservation.ConfigureServer(app);
        app.UseWebSockets();
        app.Run(async context =>
        {
            var socket = await context.AcceptWebSocketClient();
            connected?.Invoke();
            using var subscription = socket.Map(message => JsonConvert.DeserializeObject<JsonRpcMessage>(message)!)
                .Subscribe(message => receive(socket, message));
            await socket.Listen(context.RequestAborted);
        });
        await app.StartAsync();
        return app;
    }
}
