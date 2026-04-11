using System.Text;
using Newtonsoft.Json;

namespace Aiursoft.StatHub.SDK.Models;

public class CommandExecution
{
    [JsonProperty("commandId")]
    public Guid CommandId { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("stdout")]
    public string Stdout { get; set; } = string.Empty;

    [JsonProperty("stderr")]
    public string Stderr { get; set; } = string.Empty;

    [JsonProperty("exitCode")]
    public int? ExitCode { get; set; }

    [JsonProperty("startedAt")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("finishedAt")]
    public DateTime? FinishedAt { get; set; }

    [JsonProperty("isRunning")]
    public bool IsRunning { get; set; }

    [JsonIgnore]
    public StringBuilder StdoutBuilder { get; } = new();

    [JsonIgnore]
    public StringBuilder StderrBuilder { get; } = new();

    [JsonIgnore]
    public CancellationTokenSource? CancelTokenSource { get; set; }

    public void UpdateStrings()
    {
        Stdout = StdoutBuilder.ToString();
        Stderr = StderrBuilder.ToString();
    }
}
