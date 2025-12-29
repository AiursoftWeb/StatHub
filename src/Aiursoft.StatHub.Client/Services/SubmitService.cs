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
    ServerAccess serverAccess,
    ILogger<SubmitService> logger)
    : IConsumer<DstatResult[]>
{
    private async Task SubmitAsync(DstatResult[] statResults)
    {
        logger.LogInformation("Gathering metrics...");

        var bootTime = await bootTimeService.GetBootTimeAsync();
        logger.LogTrace($"Boot time: {bootTime}.");

        var hostname = await hostnameService.GetHostnameAsync();
        logger.LogTrace($"Hostname: {hostname}.");

        var version = versionService.GetAppVersion();
        logger.LogTrace($"Version: {version}.");

        var expensiveProcess = await expensiveProcessService.GetExpensiveProcessAsync();
        logger.LogTrace($"Expensive process: {expensiveProcess}.");

        var osName = await osInfoService.GetOsInfoAsync();
        logger.LogTrace($"OS: {osName}.");

        var kernelVersion = await kernelVersionService.GetKernelVersionAsync();
        logger.LogTrace($"Kernel: {kernelVersion}.");

        var cpuCores = await skuInfoService.GetCpuCores();
        logger.LogTrace($"CPU cores: {cpuCores}.");

        var totalRam = await skuInfoService.GetTotalRamInGb();
        logger.LogTrace($"Total RAM: {totalRam}.");

        var (totalRoot, usedRoot) = await skuInfoService.GetRootDriveSizeInGb();
        logger.LogTrace($"Disk size: {usedRoot}/{totalRoot}.");

        var clientId = await clientIdService.GetClientId();
        logger.LogTrace($"Client id: {clientId}.");

        var motd = await motdService.GetMotdFirstLine();
        logger.LogTrace($"MOTD: {motd}.");

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
                    motd,
                    statResults);
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
