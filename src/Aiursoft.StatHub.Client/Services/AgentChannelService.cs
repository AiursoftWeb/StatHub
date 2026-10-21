using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.Command;
using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;
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
    SshKeyService sshKeyService,
    UfwService ufwService,
    AgentChannelConnection connection,
    ILogger<AgentChannelService> logger)
    : IConsumer<DstatResult[]>
{
    private readonly CancellationTokenSource _cts = new();

    public async Task StartAsync()
    {
        var clientId = await clientIdService.GetClientId();
        _ = Task.Run(() => connection.RunAsync(clientId, HandleCommandAsync, _cts.Token), _cts.Token);
    }

    private Task HandleCommandAsync(JsonRpcMessage message, Func<JsonRpcMessage, Task> reply)
    {
        var command = message.Params?["cmd"]?.ToString();
        return message.Id != null && !string.IsNullOrWhiteSpace(command)
            ? ExecuteCommandReactive(message.Id, command, reply)
            : Task.CompletedTask;
    }

    private async Task ExecuteCommandReactive(string id, string cmd, Func<JsonRpcMessage, Task> reply)
    {
        logger.LogInformation("Executing command {Id} reactively: {Cmd}", id, cmd);
        var runner = new LongCommandRunner(Microsoft.Extensions.Logging.Abstractions.NullLogger<LongCommandRunner>.Instance);

        // Stream stdout through the original connection.
        using var outSub = runner.Output
            .Map(line => new JsonRpcMessage { Method = "stdout-chunk", Params = JToken.FromObject(new { id, data = line + "\n" }) })
            .Subscribe(reply);

        // Stream stderr through the original connection.
        using var errSub = runner.Error
            .Map(line => new JsonRpcMessage { Method = "stderr-chunk", Params = JToken.FromObject(new { id, data = line + "\n" }) })
            .Subscribe(reply);

        try
        {
            await runner.Run("bash", $"-c \"{cmd.Replace("\"", "\\\"")}\"", Directory.GetCurrentDirectory());
            
            // Report command completion.
            var doneMessage = new JsonRpcMessage 
            { 
                Method = "exec-done", 
                Params = JToken.FromObject(new { id, exitCode = 0 }) 
            };
            await reply(doneMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command {Id}.", id);
            var errorMessage = new JsonRpcMessage 
            { 
                Method = "stderr-chunk", 
                Params = JToken.FromObject(new { id, data = $"Error: {ex.Message}\n" }) 
            };
            await reply(errorMessage);
            
            var doneMessage = new JsonRpcMessage 
            { 
                Method = "exec-done", 
                Params = JToken.FromObject(new { id, exitCode = -1 }) 
            };
            await reply(doneMessage);
        }
    }

    public async Task Consume(DstatResult[] items)
    {
        if (!connection.IsConnected)
        {
            logger.LogWarning("Skipping metrics submission because WebSocket is not connected.");
            return;
        }

        try
        {
            var metrics = await GatherMetrics(items);
            if (await connection.SendMetricsAsync(metrics))
            {
                dockerService.AcknowledgeContainers(metrics.Containers);
                logger.LogInformation("Metrics acknowledged by server via AiurObserver WebSocket.");
            }
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
        var containers = await dockerService.GetContainersForReportAsync();
        var sshKeys = await sshKeyService.GetSshKeysAsync();
        var ufwStatus = await ufwService.GetUfwStatusAsync();

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
            Containers = containers,
            SshKeys = sshKeys,
            UfwStatus = ufwStatus
        };
    }
}
