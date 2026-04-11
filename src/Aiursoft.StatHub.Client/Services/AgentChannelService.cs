using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.WebSocket;
using Aiursoft.AiurObserver.Command;
using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    IOptions<ServerConfig> serverConfigOptions,
    ILogger<AgentChannelService> logger)
    : IConsumer<DstatResult[]>
{
    private readonly ServerConfig _serverConfig = serverConfigOptions.Value;
    private ObservableWebSocket? _webSocket;
    private readonly CancellationTokenSource _cts = new();
    private bool _isConnected;

    public Task StartAsync()
    {
        _ = Task.Run(ConnectAndListenLoop, _cts.Token);
        return Task.CompletedTask;
    }

    private async Task ConnectAndListenLoop()
    {
        var retryDelay = TimeSpan.FromSeconds(1);
        var clientId = await clientIdService.GetClientId();
        var wsUrl = _serverConfig.Instance.Replace("http://", "ws://").Replace("https://", "wss://").TrimEnd('/') + $"/api/agent/channel?clientId={clientId}";

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Connecting to server WebSocket at {Url}...", wsUrl);
                _webSocket = await wsUrl.ConnectAsWebSocketServer();
                _isConnected = true;
                retryDelay = TimeSpan.FromSeconds(1);
                logger.LogInformation("Connected to server WebSocket via AiurObserver.");

                // 订阅上行消息（处理来自服务器的命令）
                using var incomingSub = _webSocket
                    .Map(m => JsonConvert.DeserializeObject<JsonRpcMessage>(m))
                    .Filter(m => m != null && m.Method == "exec")
                    .Subscribe(async rpcMessage =>
                    {
                        var id = rpcMessage!.Id;
                        var cmd = rpcMessage.Params?["cmd"]?.ToString();
                        if (id != null && !string.IsNullOrWhiteSpace(cmd))
                        {
                            await ExecuteCommandReactive(id, cmd);
                        }
                    });

                await _webSocket.Listen(_cts.Token);
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

    private async Task ExecuteCommandReactive(string id, string cmd)
    {
        logger.LogInformation("Executing command {Id} reactively: {Cmd}", id, cmd);
        var runner = new LongCommandRunner(Microsoft.Extensions.Logging.Abstractions.NullLogger<LongCommandRunner>.Instance);

        // 1. 将 STDOUT 桥接到 WebSocket
        using var outSub = runner.Output
            .Map(line => new JsonRpcMessage { Method = "stdout-chunk", Params = JToken.FromObject(new { id, data = line + "\n" }) })
            .Map(JsonConvert.SerializeObject)
            .Subscribe(_webSocket!);

        // 2. 将 STDERR 桥接到 WebSocket
        using var errSub = runner.Error
            .Map(line => new JsonRpcMessage { Method = "stderr-chunk", Params = JToken.FromObject(new { id, data = line + "\n" }) })
            .Map(JsonConvert.SerializeObject)
            .Subscribe(_webSocket!);

        try
        {
            await runner.Run("bash", $"-c \"{cmd.Replace("\"", "\\\"")}\"", Directory.GetCurrentDirectory());
            
            // 3. 执行结束
            var doneMessage = new JsonRpcMessage 
            { 
                Method = "exec-done", 
                Params = JToken.FromObject(new { id, exitCode = 0 }) 
            };
            await _webSocket!.Send(JsonConvert.SerializeObject(doneMessage));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command {Id}.", id);
            var errorMessage = new JsonRpcMessage 
            { 
                Method = "stderr-chunk", 
                Params = JToken.FromObject(new { id, data = $"Error: {ex.Message}\n" }) 
            };
            await _webSocket!.Send(JsonConvert.SerializeObject(errorMessage));
            
            var doneMessage = new JsonRpcMessage 
            { 
                Method = "exec-done", 
                Params = JToken.FromObject(new { id, exitCode = -1 }) 
            };
            await _webSocket!.Send(JsonConvert.SerializeObject(doneMessage));
        }
    }

    public async Task Consume(DstatResult[] items)
    {
        if (!_isConnected || _webSocket == null)
        {
            logger.LogWarning("Skipping metrics submission because WebSocket is not connected.");
            return;
        }

        try
        {
            var metrics = await GatherMetrics(items);
            var message = new JsonRpcMessage
            {
                Method = "metrics",
                Params = JToken.FromObject(metrics)
            };
            await _webSocket.Send(JsonConvert.SerializeObject(message));
            logger.LogInformation("Metrics sent via AiurObserver WebSocket.");
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
