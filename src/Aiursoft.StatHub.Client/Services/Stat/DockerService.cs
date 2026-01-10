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
            var psResult = await commandService.RunCommandAsync("docker", "ps --format {{.ID}}|{{.Names}}|{{.Image}}|{{.State}}|{{.Status}}|{{.Ports}}|{{.RunningFor}} --no-trunc", Path.GetTempPath());
            if (psResult.code != 0)
            {
                logger.LogWarning($"docker ps failed with exit code {psResult.code}. Error: {psResult.error}");
                return [];
            }

            var containers = new List<ContainerInfo>();
            var lines = psResult.output.Replace("\r", "").Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split("|");
                if (parts.Length < 7)
                {
                    logger.LogDebug($"Failed to parse docker ps line: {line}. Expected at least 7 parts but got {parts.Length}.");
                    continue;
                }

                containers.Add(new ContainerInfo
                {
                    Id = parts[0],
                    Name = parts[1],
                    Image = parts[2].Split('@')[0].Trim(),
                    State = parts[3],
                    Status = parts[4],
                    Ports = parts[5],
                    Uptime = parts[6],
                    IsHealthy = parts[4].Contains("(healthy)") || !parts[4].Contains("(unhealthy)") && parts[3] == "running"
                });
            }

            if (containers.Count == 0)
            {
                return [];
            }

            var statsResult = await commandService.RunCommandAsync("docker", "stats --no-stream --format {{.ID}}|{{.CPUPerc}}|{{.MemUsage}}", Path.GetTempPath());
            if (statsResult.code == 0)
            {
                var statsLines = statsResult.output.Replace("\r", "").Split("\n", StringSplitOptions.RemoveEmptyEntries);
                foreach (var statsLine in statsLines)
                {
                    var parts = statsLine.Split("|");
                    if (parts.Length < 3) continue;

                    var container = containers.FirstOrDefault(c => c.Id.StartsWith(parts[0]));
                    if (container != null)
                    {
                        container.CpuPercentage = double.Parse(parts[1].Replace("%", ""), CultureInfo.InvariantCulture);
                        // MemUsage contains both usage and limit in format "usage / limit"
                        // ParseDockerSize already handles splitting on '/' to extract usage
                        var memUsageAndLimit = parts[2];
                        container.MemoryUsage = ParseDockerSize(memUsageAndLimit);
                        // Extract the limit part (after the '/')
                        if (memUsageAndLimit.Contains('/'))
                        {
                            var limitPart = memUsageAndLimit.Split('/')[1].Trim();
                            container.MemoryLimit = ParseDockerSize(limitPart);
                        }
                    }
                }
            }
            else
            {
                logger.LogWarning($"docker stats failed with exit code {statsResult.code}. Error: {statsResult.error}");
            }

            return containers.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get docker containers. Maybe docker is not installed?");
            return [];
        }
    }

    private static long ParseDockerSize(string size)
    {
        try
        {
            if (size.Contains('/'))
            {
                size = size.Split('/')[0];
            }
            size = size.Trim().ToUpper();
            if (size.EndsWith("KIB")) return (long)(double.Parse(size[..^3].Trim(), CultureInfo.InvariantCulture) * 1024);
            if (size.EndsWith("MIB")) return (long)(double.Parse(size[..^3].Trim(), CultureInfo.InvariantCulture) * 1024 * 1024);
            if (size.EndsWith("GIB")) return (long)(double.Parse(size[..^3].Trim(), CultureInfo.InvariantCulture) * 1024 * 1024 * 1024);
            if (size.EndsWith("TIB")) return (long)(double.Parse(size[..^3].Trim(), CultureInfo.InvariantCulture) * 1024L * 1024 * 1024 * 1024);
            if (size.EndsWith("KB")) return (long)(double.Parse(size[..^2].Trim(), CultureInfo.InvariantCulture) * 1000);
            if (size.EndsWith("MB")) return (long)(double.Parse(size[..^2].Trim(), CultureInfo.InvariantCulture) * 1000 * 1000);
            if (size.EndsWith("GB")) return (long)(double.Parse(size[..^2].Trim(), CultureInfo.InvariantCulture) * 1000 * 1000 * 1000);
            if (size.EndsWith("B")) return (long)double.Parse(size[..^1].Trim(), CultureInfo.InvariantCulture);
            return (long)double.Parse(size, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }
}
