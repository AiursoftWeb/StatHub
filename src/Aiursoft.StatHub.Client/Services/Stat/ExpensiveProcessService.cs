using System.Runtime.InteropServices;
using Aiursoft.CSTools.Services;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class ExpensiveProcessService
{
    private readonly CommandService _commandService;
    public ExpensiveProcessService(CommandService commandService)
    {
        _commandService = commandService;
    }
    
    public async Task<string> GetExpensiveProcessAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var commandResult = await _commandService.RunCommandAsync("ps", "-eo cmd --sort=-%cpu", Path.GetTempPath());
            var firstMeaningfulLine = commandResult.output
                .Split("\n")
                .Where(l => !l.StartsWith("CMD", StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
            var firstWord = firstMeaningfulLine!.Split(" ", StringSplitOptions.RemoveEmptyEntries)[0];
            var process = firstWord.Split("/").Last();
            return process;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var commandResult = await _commandService.RunCommandAsync("wmic", "process get Caption,ProcessId,CommandLine", Path.GetTempPath());
            var firstMeaningfulLine = commandResult.output
                .Split("\n")
                .Where(l => !l.StartsWith("Caption", StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
            var firstWord = firstMeaningfulLine!.Split(" ", StringSplitOptions.RemoveEmptyEntries)[0];
            var process = firstWord.Split("/").Last();
            return process;
        }

        throw new NotSupportedException("Your OS is not supported!");
    }
}