using System.Net.WebSockets;
using System.Text;
using Aiursoft.AiurObserver;
using Aiursoft.StatHub.Data;
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
        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        logger.LogInformation("Agent {ClientId} connected via WebSocket.", clientId);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);

        // Task to send pending commands to the agent
        var sendTask = agent.PendingCommands
            .InNewThread()
            .Subscribe(async command =>
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    var message = new JsonRpcMessage
                    {
                        Method = "exec",
                        Id = command.Id.ToString(),
                        Params = JToken.FromObject(new { cmd = command.Text })
                    };
                    var json = JsonConvert.SerializeObject(message);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
                }
            });

        // Task to listen for messages from the agent
        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cts.Token);
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms, Encoding.UTF8);
                var message = await reader.ReadToEndAsync();
                try
                {
                    var rpcMessage = JsonConvert.DeserializeObject<JsonRpcMessage>(message);
                    if (rpcMessage == null) continue;

                    switch (rpcMessage.Method)
                    {
                        case "metrics":
                            await HandleMetrics(agent, rpcMessage.Params);
                            break;
                        case "stdout-chunk":
                            HandleStdoutChunk(agent, rpcMessage.Params);
                            break;
                        case "stderr-chunk":
                            HandleStderrChunk(agent, rpcMessage.Params);
                            break;
                        case "exec-done":
                            HandleExecDone(agent, rpcMessage.Params);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error handling message from agent {ClientId}.", clientId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation("Agent {ClientId} disconnected: {Message}", clientId, ex.Message);
        }
        finally
        {
            sendTask.Unsubscribe();
        }
    }

    private async Task HandleMetrics(Agent agent, JToken? paramsToken)
    {
        var model = paramsToken?.ToObject<MetricsAddressModel>();
        if (model == null) return;

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
        agent.Containers = model.Containers?.ToList() ?? new List<ContainerInfo>();

        if (string.IsNullOrWhiteSpace(agent.CountryCode) && !string.IsNullOrWhiteSpace(agent.Ip))
        {
            var location = await ipGeolocationService.GetLocationAsync(agent.Ip);
            if (location != null)
            {
                agent.CountryName = location.Value.CountryName;
                agent.CountryCode = location.Value.CountryCode;
            }
        }

        foreach (var stat in model.Stats ?? [])
        {
            await agent.Stats.BroadcastAsync(stat);
        }
    }

    private void HandleStdoutChunk(Agent agent, JToken? paramsToken)
    {
        var idString = paramsToken?["id"]?.ToString();
        var data = paramsToken?["data"]?.ToString();
        if (Guid.TryParse(idString, out var id) && agent.CommandHistory.TryGetValue(id, out var exec))
        {
            exec.StdoutBuilder.Append(data);
            exec.UpdateStrings();
        }
    }

    private void HandleStderrChunk(Agent agent, JToken? paramsToken)
    {
        var idString = paramsToken?["id"]?.ToString();
        var data = paramsToken?["data"]?.ToString();
        if (Guid.TryParse(idString, out var id) && agent.CommandHistory.TryGetValue(id, out var exec))
        {
            exec.StderrBuilder.Append(data);
            exec.UpdateStrings();
        }
    }

    private void HandleExecDone(Agent agent, JToken? paramsToken)
    {
        var idString = paramsToken?["id"]?.ToString();
        var exitCode = paramsToken?["exitCode"]?.ToObject<int>();
        if (Guid.TryParse(idString, out var id) && agent.CommandHistory.TryGetValue(id, out var exec))
        {
            exec.ExitCode = exitCode;
            exec.FinishedAt = DateTime.UtcNow;
            exec.IsRunning = false;
            exec.UpdateStrings();
        }
    }
}
