using Aiursoft.CSTools.Services;
using Aiursoft.CSTools.Tools;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class ExpensiveProcessService(CommandService commandService)
{
    public async Task<string> GetExpensiveProcessAsync()
    {
        // In docker, this will return the most expensive process of the host machine.
        var commandResult = await commandService.RunCommandAsync("ps", "-eo cmd --sort=-%cpu", Path.GetTempPath());
        var firstMeaningfulLine = commandResult.output
            .Split("\n")
            .Where(l => !l.StartsWith("CMD", StringComparison.InvariantCultureIgnoreCase))
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
        var firstWord = firstMeaningfulLine!.Split(" ", StringSplitOptions.RemoveEmptyEntries)[0];
        var process = firstWord
            .Split("/")
            .Last()
            .Split(".")
            .First()
            .SafeSubstring(16);
        return process;
    }
}
