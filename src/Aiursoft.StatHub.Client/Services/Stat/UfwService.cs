using Aiursoft.Canon;
using Aiursoft.CSTools.Services;
using Aiursoft.StatHub.SDK.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class UfwService(
    CacheService cacheService,
    CommandService commandService,
    ILogger<UfwService> logger)
{
    public Task<UfwStatus> GetUfwStatusAsync()
    {
        return cacheService.RunWithCache("ufw-status", async () =>
        {
            try
            {
                var result = await commandService.RunCommandAsync("ufw", "status", Path.GetTempPath());
                var output = result.output.Trim();

                if (result.code != 0 || string.IsNullOrWhiteSpace(output))
                {
                    logger.LogDebug("ufw command returned non-zero or empty output. Code: {Code}", result.code);
                    return new UfwStatus { IsEnabled = false, RawOutput = output };
                }

                var isEnabled = output.Contains("Status: active");

                var openPorts = new List<string>();
                if (isEnabled)
                {
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        // Skip header lines and separators
                        if (trimmed.StartsWith("Status:") ||
                            trimmed.StartsWith("To") ||
                            trimmed.StartsWith("--"))
                            continue;

                        // Lines look like: "22/tcp                     ALLOW       Anywhere"
                        // or: "22/tcp (v6)                ALLOW       Anywhere (v6)"
                        var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && parts[1].Equals("ALLOW", StringComparison.OrdinalIgnoreCase))
                        {
                            var portSpec = parts[0]; // e.g., "22/tcp" or "22"
                            openPorts.Add(portSpec);
                        }
                    }
                }

                return new UfwStatus
                {
                    IsEnabled = isEnabled,
                    OpenPorts = openPorts,
                    RawOutput = output
                };
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to get ufw status. Maybe ufw is not installed?");
                return new UfwStatus { IsEnabled = false };
            }
        }, cachedMinutes: _ => TimeSpan.FromMinutes(20));
    }
}
