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
                return [];
            }

            var psResult = await commandService.RunCommandAsync("docker", "ps --format \"{{.ID}}\t{{.Names}}\t{{.Image}}\t{{.State}}\t{{.Status}}\t{{.Ports}}\t{{.RunningFor}}\" --no-trunc", Path.GetTempPath());
            if (psResult.code != 0)
            {
                return [];
            }

            var containers = new List<ContainerInfo>();
            var lines = psResult.output.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split("\t");
                if (parts.Length < 7) continue;

                containers.Add(new ContainerInfo
                {
                    Id = parts[0],
                    Name = parts[1],
                    Image = parts[2],
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

            var statsResult = await commandService.RunCommandAsync("docker", "stats --no-stream --format \"{{.ID}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemLimit}}\"", Path.GetTempPath());
            if (statsResult.code == 0)
            {
                var statsLines = statsResult.output.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                foreach (var statsLine in statsLines)
                {
                    var parts = statsLine.Split("\t");
                    if (parts.Length < 4) continue;

                    var container = containers.FirstOrDefault(c => c.Id.StartsWith(parts[0]));
                    if (container != null)
                    {
                        container.CpuPercentage = double.Parse(parts[1].Replace("%", ""));
                        container.MemoryUsage = ParseDockerSize(parts[2]);
                        container.MemoryLimit = ParseDockerSize(parts[3]);
                    }
                }
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
            size = size.Trim().ToUpper();
            if (size.EndsWith("KIB")) return (long)(double.Parse(size.Replace("KIB", "").Trim()) * 1024);
            if (size.EndsWith("MIB")) return (long)(double.Parse(size.Replace("MIB", "").Trim()) * 1024 * 1024);
            if (size.EndsWith("GIB")) return (long)(double.Parse(size.Replace("GIB", "").Trim()) * 1024 * 1024 * 1024);
            if (size.EndsWith("TIB")) return (long)(double.Parse(size.Replace("TIB", "").Trim()) * 1024L * 1024 * 1024 * 1024);
            if (size.EndsWith("KB")) return (long)(double.Parse(size.Replace("KB", "").Trim()) * 1000);
            if (size.EndsWith("MB")) return (long)(double.Parse(size.Replace("MB", "").Trim()) * 1000 * 1000);
            if (size.EndsWith("GB")) return (long)(double.Parse(size.Replace("GB", "").Trim()) * 1000 * 1000 * 1000);
            if (size.EndsWith("B")) return (long)double.Parse(size.Replace("B", "").Trim());
            return (long)double.Parse(size);
        }
        catch
        {
            return 0;
        }
    }
}
