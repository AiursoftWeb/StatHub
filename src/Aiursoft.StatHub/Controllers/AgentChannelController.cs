using Aiursoft.StatHub.Services.MetricsExport;
using Aiursoft.AiurObserver;
using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.StatHub.Services.BackgroundJobs;
using Aiursoft.AiurObserver.WebSocket.Server;
using Aiursoft.StatHub.Data;
using Aiursoft.StatHub.Services.Monitoring;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.SDK.Models;
using Aiursoft.StatHub.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aiursoft.StatHub.Controllers;

[AllowAnonymous]
[Route("api/agent")]
public class AgentChannelController(
    InMemoryDatabase database,
    MetricsExportBuffer metricsExport,
    IServiceScopeFactory scopeFactory,
    BackgroundJobRegistry backgroundJobs,
    IpGeolocationService ipGeolocationService,
    ILogger<AgentChannelController> logger) : ControllerBase
{
    [Route("channel")]
    [EnforceWebSocket]
    public async Task Channel([FromQuery] string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        var agent = database.GetOrAddClient(clientId);
        var pusher = await HttpContext.AcceptWebSocketClient();
        logger.LogInformation("Agent {ClientId} connected via ObservableWebSocket.", clientId);
        var sendGate = new SemaphoreSlim(1, 1);
        async Task Reply(string message)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                await sendGate.WaitAsync(timeout.Token);
                try
                {
                    await pusher.Send(message, timeout.Token).WaitAsync(timeout.Token);
                    if (!pusher.Connected) throw new IOException("The agent connection closed during send.");
                }
                finally { sendGate.Release(); }
            }
            catch
            {
                HttpContext.Abort();
                throw;
            }
        }

        // Commands and acknowledgements share one serialized writer.
        using var downSubscription = agent.PendingCommands
            .Map(command => new JsonRpcMessage
            {
                Method = "exec",
                Id = command.Id.ToString(),
                Params = JToken.FromObject(new { cmd = command.Text })
            })
            .Map(JsonConvert.SerializeObject)
            .Subscribe(Reply);

        // 上行流：WebSocket 输入 -> 反序列化 -> Handler
        using var upSubscription = pusher
            .Map(m => JsonConvert.DeserializeObject<JsonRpcMessage>(m))
            .Filter(m => m != null)
            .Subscribe(async rpcMessage => await HandleRpcMessage(agent, rpcMessage!, Reply));

        try
        {
            await pusher.Listen(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogInformation("Agent {ClientId} connection closed: {Message}", clientId, ex.Message);
        }
    }

    private async Task HandleRpcMessage(Agent agent, JsonRpcMessage rpcMessage, Func<string, Task> reply)
    {
        try
        {
            var p = rpcMessage.Params;
            switch (rpcMessage.Method)
            {
                case "metrics":
                    var metrics = p?.ToObject<MetricsAddressModel>();
                    await HandleMetricsPipe(agent, metrics);
                    if (metrics?.Stats != null && !string.IsNullOrWhiteSpace(rpcMessage.Id))
                        await reply(JsonConvert.SerializeObject(new JsonRpcMessage
                        {
                            Id = rpcMessage.Id, Method = "metrics-ack"
                        }));
                    break;
                case "stdout-chunk":
                    await HandleOutputChunk(agent, p, true);
                    break;
                case "stderr-chunk":
                    await HandleOutputChunk(agent, p, false);
                    break;
                case "exec-done":
                    HandleExecDone(agent, p);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling RPC message.");
        }
    }

    private async Task HandleMetricsPipe(Agent agent, MetricsAddressModel? model)
    {
        if (model == null) return;

        // 基础信息同步 (同步调用)
        SyncAgentMetadata(agent, model);

        // Preserve sample order so monitoring observes transient recovery within a batch.
        if (model.Stats != null)
        {
            var observations = new List<string>();
            foreach (var sample in model.Stats)
            {
                metricsExport.Capture(agent.ClientId, agent.Hostname, sample, agent.LastUpdate);
                await agent.Stats.BroadcastAsync(sample);
                if (agent.CpuCores <= 0 || sample.CpuIdl is < 0 or > 100) continue;
                observations.Add(agent.GetHealthReport().Status switch
                {
                    AgentStatus.Healthy => "green", AgentStatus.Warning => "yellow",
                    AgentStatus.Critical => "red", _ => "unknown"
                });
            }
            using var scope = scopeFactory.CreateScope();
            var wakeRecovery = await scope.ServiceProvider.GetRequiredService<MonitoringService>()
                .ObserveAsync(agent.ClientId, agent.Hostname, observations);
            if (wakeRecovery) backgroundJobs.TriggerNow<MonitoringJob>();
        }
    }

    private void SyncAgentMetadata(Agent agent, MetricsAddressModel model)
    {
        agent.BootTime = model.BootTime;
        agent.Hostname = model.Hostname ?? "Unknown";
        agent.Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        agent.LastUpdate = DateTime.UtcNow;
        agent.Version = model.Version ?? "Unknown";
        agent.Process = model.Process ?? "Unknown";
        agent.OsName = model.OsName ?? "Unknown";
        agent.KernelVersion = model.KernelVersion;
        agent.CpuCores = model.CpuCores;
        agent.RamInGb = model.RamInGb;
        agent.UsedRoot = model.UsedRoot;
        agent.TotalRoot = model.TotalRoot;
        agent.Disks = model.Disks.ToList();
        agent.Motd = model.Motd;
        if (model.Containers != null)
            agent.Containers = model.Containers.ToList();
        agent.SshKeys = model.SshKeys?.ToList() ?? new List<SshKeyInfo>();
        agent.UfwStatus = model.UfwStatus;

        if (string.IsNullOrWhiteSpace(agent.CountryCode) && !string.IsNullOrWhiteSpace(agent.Ip))
        {
            _ = Task.Run(async () =>
            {
                var location = await ipGeolocationService.GetLocationAsync(agent.Ip);
                if (location != null)
                {
                    agent.CountryName = location.Value.CountryName;
                    agent.CountryCode = location.Value.CountryCode;
                }
            });
        }
    }

    private async Task HandleOutputChunk(Agent agent, JToken? p, bool isStdout)
    {
        var idString = p?["id"]?.ToString();
        var data = p?["data"]?.ToString();
        if (Guid.TryParse(idString, out var id) && agent.CommandHistory.TryGetValue(id, out var exec))
        {
            // 不再手动操作 StringBuilder，而是推入对应的响应式管道
            if (isStdout)
                await exec.StdoutStream.BroadcastAsync(data ?? string.Empty);
            else
                await exec.StderrStream.BroadcastAsync(data ?? string.Empty);
        }
    }

    private void HandleExecDone(Agent agent, JToken? p)
    {
        var idString = p?["id"]?.ToString();
        var exitCode = p?["exitCode"]?.ToObject<int>();
        if (Guid.TryParse(idString, out var id) && agent.CommandHistory.TryGetValue(id, out var exec))
        {
            exec.ExitCode = exitCode;
            exec.FinishedAt = DateTime.UtcNow;
            exec.IsRunning = false;
        }
    }
}
