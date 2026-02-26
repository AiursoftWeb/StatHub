using Aiursoft.AiurObserver;
using Aiursoft.StatHub.Client.Services.Stat;
using Aiursoft.StatHub.SDK;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

public class SubmitService(
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
    ILogger<SubmitService> logger)
    : IConsumer<DstatResult[]>
{
    private async Task SubmitAsync(DstatResult[] statResults)
    {
        logger.LogInformation("Gathering metrics...");

        var bootTime = await bootTimeService.GetBootTimeAsync();
        logger.LogTrace("Boot time: {BootTime}.", bootTime);

        var hostname = await hostnameService.GetHostnameAsync();
        logger.LogTrace("Hostname: {Hostname}.", hostname);

        var version = versionService.GetAppVersion();
        logger.LogTrace("Version: {Version}.", version);

        var expensiveProcess = await expensiveProcessService.GetExpensiveProcessAsync();
        logger.LogTrace("Expensive process: {ExpensiveProcess}.", expensiveProcess);

        var osName = await osInfoService.GetOsInfoAsync();
        logger.LogTrace("OS: {OsName}.", osName);

        var kernelVersion = await kernelVersionService.GetKernelVersionAsync();
        logger.LogTrace("Kernel: {KernelVersion}.", kernelVersion);

        var cpuCores = await skuInfoService.GetCpuCores();
        logger.LogTrace("CPU cores: {CpuCores}.", cpuCores);

        var totalRam = await skuInfoService.GetTotalRamInGb();
        logger.LogTrace("Total RAM: {TotalRam}.", totalRam);

        var disks = await skuInfoService.GetDisksSpace();
        var rootDisk = disks.FirstOrDefault(d => d.Name == "/") ?? disks.FirstOrDefault();
        var totalRoot = rootDisk?.Total ?? 0;
        var usedRoot = rootDisk?.Used ?? 0;
        logger.LogTrace("Disk size: {UsedRoot}/{TotalRoot}. Found {DisksLength} disks.", usedRoot, totalRoot, disks.Length);

        var clientId = await clientIdService.GetClientId();
        logger.LogTrace("Client id: {ClientId}.", clientId);

        var motd = await motdService.GetMotdFirstLine();
        logger.LogTrace("MOTD: {Motd}.", motd);

        var containers = await dockerService.GetDockerContainersAsync();
        logger.LogTrace("Found {ContainersLength} containers.", containers.Length);

        logger.LogTrace("Sending metrics...");
        try
        {
            var response =
                await serverAccess.MetricsAsync(
                    clientId,
                    hostname,
                    bootTime,
                    version,
                    expensiveProcess,
                    osName,
                    kernelVersion,
                    cpuCores,
                    totalRam,
                    usedRoot,
                    totalRoot,
                    disks,
                    motd,
                    statResults,
                    containers);
            logger.LogInformation("Metrics sent! Response: {ResponseMessage}.", response.Message);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to send metrics!");
            throw;
        }
    }

    public Task Consume(DstatResult[] items)
    {
        return SubmitAsync(items);
    }
}
