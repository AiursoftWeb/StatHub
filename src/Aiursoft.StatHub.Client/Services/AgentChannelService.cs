using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Aiursoft.AiurObserver;
using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aiursoft.StatHub.Client.Services;

public class AgentChannelService(
    MotdService motdService,
    ClientIdService clientIdService,
    SkuInfoService skuInfoService,
    OsInfoService osInfoService,
    KernelVersionService kernelVersionService,
    ExpensiveProcessService expensiveProcessService,
    VersionService versionService,
    HostnameService hostnameService,
    BootTimeService bootTimeService,
    DockerService dockerService,
    ServerAccess serverAccess,
    ServerConfig serverConfig,
    ILogger<AgentChannelService> logger)
    : IConsumer<DstatResult[]>
{
    private ClientWebSocket? _webSocket;
    private readonly CancellationTokenSource _cts = new();
    private bool _isConnected;

    public async Task StartAsync()
    {
        _ = Task.Run(ConnectAndListenLoop, _cts.Token);
        await Task.CompletedTask;
    }

    private async Task ConnectAndListenLoop()
    {
        var retryDelay = TimeSpan.FromSeconds(1);
        var clientId = await clientIdService.GetClientId();
        var wsUrl = serverConfig.Instance.Replace("http://", "ws://").Replace("https://", "wss://").TrimEnd('/') + $"/api/agent/channel?clientId={clientId}";

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Connecting to server WebSocket at {Url}...", wsUrl);
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(wsUrl), _cts.Token);
                _isConnected = true;
                retryDelay = TimeSpan.FromSeconds(1);
                logger.LogInformation("Connected to server WebSocket.");

                await ListenLoop();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                logger.LogWarning("WebSocket connection error: {Message}. Retrying in {Delay}s...", ex.Message, retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, _cts.Token);
                retryDelay = TimeSpan.FromSeconds(Math.Min(60, retryDelay.TotalSeconds * 2));
            }
        }
    }

    private async Task ListenLoop()
    {
        var buffer = new byte[1024 * 4];
        while (_webSocket?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token);
                    _isConnected = false;
                    return;
                }
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms, Encoding.UTF8);
            var message = await reader.ReadToEndAsync();
            await HandleMessage(message);
        }
    }

    private async Task HandleMessage(string messageJson)
    {
        try
        {
            var rpcMessage = JsonConvert.DeserializeObject<JsonRpcMessage>(messageJson);
            if (rpcMessage == null) return;

            if (rpcMessage.Method == "exec")
            {
                var id = rpcMessage.Id;
                var cmd = rpcMessage.Params?["cmd"]?.ToString();
                if (id != null && !string.IsNullOrWhiteSpace(cmd))
                {
                    _ = Task.Run(() => ExecuteCommand(id, cmd), _cts.Token);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling message from server.");
        }
    }

    private async Task ExecuteCommand(string id, string cmd)
    {
        logger.LogInformation("Executing command {Id}: {Cmd}", id, cmd);
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{cmd.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += async (_, e) =>
            {
                if (e.Data != null)
                {
                    await SendRpcMessage("stdout-chunk", new { id, data = e.Data + "\n" });
                }
            };

            process.ErrorDataReceived += async (_, e) =>
            {
                if (e.Data != null)
                {
                    await SendRpcMessage("stderr-chunk", new { id, data = e.Data + "\n" });
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(_cts.Token);
            await SendRpcMessage("exec-done", new { id, exitCode = process.ExitCode });
            logger.LogInformation("Command {Id} finished with exit code {ExitCode}.", id, process.ExitCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command {Id}.", id);
            await SendRpcMessage("stderr-chunk", new { id, data = $"Error: {ex.Message}\n" });
            await SendRpcMessage("exec-done", new { id, exitCode = -1 });
        }
    }

    private async Task SendRpcMessage(string method, object @params)
    {
        if (!_isConnected || _webSocket?.State != WebSocketState.Open) return;

        try
        {
            var message = new JsonRpcMessage
            {
                Method = method,
                Params = JToken.FromObject(@params)
            };
            var json = JsonConvert.SerializeObject(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending RPC message {Method}.", method);
        }
    }

    public async Task Consume(DstatResult[] items)
    {
        if (!_isConnected)
        {
            logger.LogWarning("Skipping metrics submission because WebSocket is not connected.");
            return;
        }

        try
        {
            var metrics = await GatherMetrics(items);
            await SendRpcMessage("metrics", metrics);
            logger.LogInformation("Metrics sent via WebSocket.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error gathering or sending metrics.");
        }
    }

    private async Task<MetricsAddressModel> GatherMetrics(DstatResult[] statResults)
    {
        var bootTime = await bootTimeService.GetBootTimeAsync();
        var hostname = await hostnameService.GetHostnameAsync();
        var version = versionService.GetAppVersion();
        var expensiveProcess = await expensiveProcessService.GetExpensiveProcessAsync();
        var osName = await osInfoService.GetOsInfoAsync();
        var kernelVersion = await kernelVersionService.GetKernelVersionAsync();
        var cpuCores = await skuInfoService.GetCpuCores();
        var totalRam = await skuInfoService.GetTotalRamInGb();
        var disks = await skuInfoService.GetDisksSpace();
        var rootDisk = disks.FirstOrDefault(d => d.Name == "/") ?? disks.FirstOrDefault();
        var totalRoot = rootDisk?.Total ?? 0;
        var usedRoot = rootDisk?.Used ?? 0;
        var clientId = await clientIdService.GetClientId();
        var motd = await motdService.GetMotdFirstLine();
        var containers = await dockerService.GetDockerContainersAsync();

        return new MetricsAddressModel
        {
            ClientId = clientId,
            Hostname = hostname,
            BootTime = bootTime,
            Version = version,
            Process = expensiveProcess,
            OsName = osName,
            KernelVersion = kernelVersion,
            CpuCores = cpuCores,
            RamInGb = totalRam,
            UsedRoot = usedRoot,
            TotalRoot = totalRoot,
            Disks = disks,
            Motd = motd,
            Stats = statResults,
            Containers = containers
        };
    }
}
