using System.Runtime.InteropServices;
using Aiursoft.CSTools.Services;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class HostnameService
{
    private readonly CommandService _commandService;

    public HostnameService(CommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<string> GetHostnameAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var hostname = await File.ReadAllTextAsync("/etc/hostname");
            return hostname.Trim();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var commandResult = await _commandService.RunCommandAsync("hostname", string.Empty, Path.GetTempPath());
            return commandResult.output.Trim();
        }

        throw new NotSupportedException("Your OS is not supported!");
    }
}