using System.Globalization;
using System.Runtime.InteropServices;
using Aiursoft.CSTools.Services;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class UptimeService
{
    private readonly CommandService _commandService;

    public UptimeService(CommandService commandService)
    {
        _commandService = commandService;
    }
    
    public async Task<int> GetUpTimeAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var uptime = await File.ReadAllTextAsync("/proc/uptime");
            var upSeconds = double.Parse(uptime.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);
            return (int)upSeconds;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var commandResult = await _commandService.RunCommandAsync("systeminfo", string.Empty, Path.GetTempPath());
            var upTime = commandResult.output.Split("\n").FirstOrDefault(t => t.StartsWith("System Boot Time:"));
            var uptimeDateString = upTime!.Substring("System Boot Time:".Length).Trim();
            var upTimeDate = DateTime.Parse(uptimeDateString, CultureInfo.InvariantCulture);
            var upTimeSpan = DateTime.Now - upTimeDate;
            return (int)upTimeSpan.TotalSeconds;
        }
        
        throw new NotSupportedException("Your OS is not supported!");
    }
}