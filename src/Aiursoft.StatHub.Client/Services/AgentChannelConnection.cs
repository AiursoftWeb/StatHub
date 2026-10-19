using System.Net.WebSockets;
using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.WebSocket;
using Aiursoft.StatHub.SDK;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aiursoft.StatHub.Client.Services;

public sealed class AgentChannelConnection(IOptions<ServerConfig> options, ILogger<AgentChannelConnection> logger)
{
    private Session? _session;
    public bool IsConnected => Volatile.Read(ref _session)?.Socket.Connected == true;

    public async Task RunAsync(string clientId, Func<JsonRpcMessage, Func<JsonRpcMessage, Task>, Task> onCommand,
        CancellationToken token)
    {
        var endpoint = options.Value.Instance.Replace("http://", "ws://").Replace("https://", "wss://")
            .TrimEnd('/') + $"/api/agent/channel?clientId={Uri.EscapeDataString(clientId)}";
        var retryDelay = TimeSpan.FromSeconds(1);
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var transport = new ClientWebSocket();
                transport.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                transport.Options.KeepAliveTimeout = TimeSpan.FromSeconds(15);
                using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
                using var abort = lifetime.Token.Register(transport.Abort);
                using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                connectTimeout.CancelAfter(TimeSpan.FromSeconds(15));
                logger.LogInformation("Connecting to server WebSocket at {Url}...", endpoint);
                await transport.ConnectAsync(new Uri(endpoint), connectTimeout.Token);
                var session = new Session(transport, new ObservableWebSocket(transport), lifetime);
                Volatile.Write(ref _session, session);
                logger.LogInformation("Connected to server WebSocket via AiurObserver.");
                try
                {
                    using var subscription = session.Socket
                        .Map(message => JsonConvert.DeserializeObject<JsonRpcMessage>(message))
                        .Filter(message => message != null)
                        .Subscribe(message =>
                        {
                            if (message!.Method == "metrics-ack")
                            {
                                var pending = Volatile.Read(ref session.Pending);
                                if (pending != null && message.Id == pending.Id)
                                {
                                    pending.Completion.TrySetResult();
                                    retryDelay = TimeSpan.FromSeconds(1);
                                }
                            }
                            else if (message.Method == "exec")
                            {
                                // Long-running commands must not block receipt of metrics acknowledgements.
                                _ = HandleCommandAsync(message, reply => SendAsync(session, reply, session.Token), onCommand);
                            }
                            return Task.CompletedTask;
                        });
                    await session.Socket.Listen(session.Token);
                }
                finally
                {
                    Interlocked.CompareExchange(ref _session, null, session);
                    session.Stop();
                    Volatile.Read(ref session.Pending)?.Completion.TrySetCanceled();
                }
                if (!token.IsCancellationRequested)
                    logger.LogWarning("Server WebSocket closed. Reconnecting in {Delay}s...", retryDelay.TotalSeconds);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "WebSocket connection failed. Reconnecting in {Delay}s...", retryDelay.TotalSeconds);
            }

            try { await Task.Delay(retryDelay, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            retryDelay = TimeSpan.FromSeconds(Math.Min(60, retryDelay.TotalSeconds * 2));
        }
    }

    public async Task<bool> SendMetricsAsync(MetricsAddressModel metrics)
    {
        var session = Volatile.Read(ref _session);
        if (session == null || !await session.ReportGate.WaitAsync(0)) return false;
        var pending = new PendingReport(Guid.NewGuid().ToString("N"));
        Volatile.Write(ref session.Pending, pending);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(session.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            var request = new JsonRpcMessage { Id = pending.Id, Method = "metrics", Params = JToken.FromObject(metrics) };
            await SendAsync(session, request, timeout.Token);
            await pending.Completion.Task.WaitAsync(timeout.Token);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Metrics were not acknowledged within 30 seconds or the connection failed; reconnecting.");
            session.Stop();
            return false;
        }
        finally
        {
            Interlocked.CompareExchange(ref session.Pending, null, pending);
            session.ReportGate.Release();
        }
    }

    private async Task HandleCommandAsync(JsonRpcMessage request, Func<JsonRpcMessage, Task> reply,
        Func<JsonRpcMessage, Func<JsonRpcMessage, Task>, Task> onCommand)
    {
        try { await onCommand(request, reply); }
        catch (Exception ex) { logger.LogWarning(ex, "Command {Id} could not complete on its original connection.", request.Id); }
    }

    private static async Task SendAsync(Session session, JsonRpcMessage message, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token, session.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            await session.SendGate.WaitAsync(timeout.Token);
            try
            {
                await session.Socket.Send(JsonConvert.SerializeObject(message), timeout.Token);
                // Older releases swallowed send failures. They must never count as delivery.
                if (!session.Socket.Connected) throw new WebSocketException("The server connection closed during send.");
            }
            finally { session.SendGate.Release(); }
        }
        catch
        {
            session.Stop();
            throw;
        }
    }

    private sealed record PendingReport(string Id)
    {
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class Session(ClientWebSocket transport, ObservableWebSocket socket, CancellationTokenSource lifetime)
    {
        private int _stopped;
        public ObservableWebSocket Socket { get; } = socket;
        public CancellationTokenSource Lifetime { get; } = lifetime;
        public CancellationToken Token { get; } = lifetime.Token;
        public SemaphoreSlim ReportGate { get; } = new(1, 1);
        public SemaphoreSlim SendGate { get; } = new(1, 1);
        public PendingReport? Pending;

        public void Stop()
        {
            if (Interlocked.Exchange(ref _stopped, 1) != 0) return;
            try { Lifetime.Cancel(); }
            finally { transport.Abort(); }
        }
    }
}
