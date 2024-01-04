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
            var commandResult = await _commandService.RunCommandAsync("ps", "-eo cmd --sort=-%cpu", Path.GetTempPath());
            var firstMeaningfulLine = commandResult.output
                .Split("\n")
                .Where(l => !l.StartsWith("CMD", StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
            var firstWord = firstMeaningfulLine!.Split(" ", StringSplitOptions.RemoveEmptyEntries)[0];
            var process = firstWord.Split("/").Last();
            return process;
    }
}