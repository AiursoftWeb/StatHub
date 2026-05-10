using System.Globalization;
using Aiursoft.CSTools.Services;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class DockerService(
    CommandService commandService,
    ILogger<DockerService> logger)
{
    public async Task<ContainerInfo[]> GetDockerContainersAsync()
    {
        try
        {
            var dockerCheck = await commandService.RunCommandAsync("docker", "--version", Path.GetTempPath());
            if (dockerCheck.code != 0)
            {
                logger.LogDebug("Docker is not installed or not working.");
                return [];
            }

            // Do NOT use spaces in the format string. If there are no spaces, it works better with some command runners.
            var psResult = await commandService.RunCommandAsync("docker", "ps -s --format {{.ID}}|{{.Names}}|{{.Image}}|{{.State}}|{{.Status}}|{{.Ports}}|{{.RunningFor}}|{{.CreatedAt}}|{{.Size}} --no-trunc", Path.GetTempPath());
            if (psResult.code != 0)
            {
                logger.LogWarning("docker ps failed with exit code {Code}. Error: {Error}", psResult.code, psResult.error);
                return [];
            }

            var containers = new List<ContainerInfo>();
            var lines = psResult.output.Replace("\r", "").Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split("|");
                if (parts.Length < 9)
                {
                    logger.LogDebug("Failed to parse docker ps line: {Line}. Expected at least 9 parts but got {Length}.", line, parts.Length);
                    continue;
                }

                var createdTimeStr = parts[7];
                // Example: 2024-05-22 14:12:57 +0800 CST
                if (!DateTime.TryParse(createdTimeStr, out var createdTime))
                {
                    // Fallback: try to parse the first 19 characters (yyyy-MM-dd HH:mm:ss)
                    if (createdTimeStr.Length >= 19 && DateTime.TryParse(createdTimeStr[..19], out var fallbackTime))
                    {
                        createdTime = fallbackTime;
                    }
                    else
                    {
                        createdTime = DateTime.MinValue;
                    }
                }

                var status = parts[4];
                var hasHealthCheck = status.Contains("(healthy)") || status.Contains("(unhealthy)") || status.Contains("health: starting");
                
                var sizePart = parts[8];
                var diskUsage = sizePart;
                var imageSize = string.Empty;
                if (sizePart.Contains(" (virtual "))
                {
                    var sizeParts = sizePart.Split(" (virtual ");
                    diskUsage = sizeParts[0];
                    imageSize = sizeParts[1].TrimEnd(')');
                }
                
                containers.Add(new ContainerInfo
                {
                    Id = parts[0],
                    Name = parts[1].Split('.')[0],
                    Image = parts[2].Split('@')[0].Trim(),
                    State = parts[3],
                    Status = status,
                    Ports = parts[5],
                    Uptime = parts[6],
                    CreatedTime = createdTime,
                    DiskUsage = diskUsage,
                    DiskUsageBytes = DockerNumberProcessor.ParseDockerSize(diskUsage),
                    ImageSize = imageSize,
                    ImageSizeBytes = DockerNumberProcessor.ParseDockerSize(imageSize),
                    HasHealthCheck = hasHealthCheck,
                    IsHealthy = hasHealthCheck ? status.Contains("(healthy)") : parts[3] == "running"
                });
            }

            if (containers.Count == 0)
            {
                return [];
            }

            var statsResult = await commandService.RunCommandAsync("docker", "stats --no-stream --format {{.ID}}|{{.CPUPerc}}|{{.MemUsage}}|{{.BlockIO}}|{{.NetIO}}", Path.GetTempPath());
            if (statsResult.code == 0)
            {
                var statsLines = statsResult.output.Replace("\r", "").Split("\n", StringSplitOptions.RemoveEmptyEntries);
                foreach (var statsLine in statsLines)
                {
                    var parts = statsLine.Split("|");
                    if (parts.Length < 5) continue;

                    var container = containers.FirstOrDefault(c => c.Id.StartsWith(parts[0]));
                    if (container != null)
                    {
                        if (double.TryParse(parts[1].Replace("%", ""), CultureInfo.InvariantCulture, out var cpu))
                        {
                            container.CpuPercentage = cpu;
                        }
                        
                        // MemUsage contains both usage and limit in format "usage / limit"
                        var memUsageAndLimit = parts[2];
                        container.MemoryUsage = DockerNumberProcessor.ParseDockerSize(memUsageAndLimit);
                        if (memUsageAndLimit.Contains('/'))
                        {
                            var limitPart = memUsageAndLimit.Split('/')[1].Trim();
                            container.MemoryLimit = DockerNumberProcessor.ParseDockerSize(limitPart);
                        }
                        
                        container.BlockIo = parts[3];
                        if (parts[3].Contains('/'))
                        {
                            var ioParts = parts[3].Split('/');
                            container.BlockIoRead = DockerNumberProcessor.ParseDockerSize(ioParts[0].Trim());
                            container.BlockIoWrite = DockerNumberProcessor.ParseDockerSize(ioParts[1].Trim());
                        }

                        container.NetIo = parts[4];
                        if (parts[4].Contains('/'))
                        {
                            var netParts = parts[4].Split('/');
                            container.NetIoIn = DockerNumberProcessor.ParseDockerSize(netParts[0].Trim());
                            container.NetIoOut = DockerNumberProcessor.ParseDockerSize(netParts[1].Trim());
                        }
                    }
                }
            }
            else
            {
                logger.LogWarning("docker stats failed with exit code {Code}. Error: {Error}", statsResult.code, statsResult.error);
            }

            return containers.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get docker containers. Maybe docker is not installed?");
            return [];
        }
    }
}
