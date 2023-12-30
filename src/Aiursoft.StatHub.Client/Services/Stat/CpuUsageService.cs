using System.Globalization;
using System.Runtime.InteropServices;
using Aiursoft.CSTools.Services;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class CpuUsageService
{
    private readonly CommandService _commandService;

    public CpuUsageService(CommandService commandService)
    {
        _commandService = commandService;
    }

    /// <summary>
    /// Get CPU usage in percentage.
    /// </summary>
    /// <returns>CPU Usage, output value is from 0 to 100.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task<int> GetCpuUsageAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var dstatResult = await _commandService.RunCommandAsync("dstat", "-c --noheader 1 1", Path.GetTempPath());
            var usage = dstatResult.output.Split("\n").Last(t => !string.IsNullOrWhiteSpace(t));
            var idl = usage.Split(" ", StringSplitOptions.RemoveEmptyEntries)[2];
            var idlDouble = int.Parse(idl, CultureInfo.InvariantCulture);
            return 100 - idlDouble;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var commandResult =
                await _commandService.RunCommandAsync("wmic", "cpu get loadpercentage", Path.GetTempPath());
            var usage = commandResult.output.Split("\n").Last(t => !string.IsNullOrWhiteSpace(t));
            var usageDouble = int.Parse(usage, CultureInfo.InvariantCulture);
            return usageDouble;
        }

        throw new NotSupportedException("Your OS is not supported!");
    }
}