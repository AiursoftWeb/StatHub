using System.Text;
using Aiursoft.AiurObserver;
using Newtonsoft.Json;

namespace Aiursoft.StatHub.SDK.Models;

public class CommandExecution
{
    public CommandExecution()
    {
        StdoutStream = new AsyncObservable<string>();
        StderrStream = new AsyncObservable<string>();

        // 纯粹的内部状态同步：订阅流来更新 StringBuilder
        StdoutStream.Subscribe(data =>
        {
            StdoutBuilder.Append(data);
            Stdout = StdoutBuilder.ToString();
            return Task.CompletedTask;
        });

        StderrStream.Subscribe(data =>
        {
            StderrBuilder.Append(data);
            Stderr = StderrBuilder.ToString();
            return Task.CompletedTask;
        });
    }

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

    // 响应式管道
    [JsonIgnore]
    public AsyncObservable<string> StdoutStream { get; }
    [JsonIgnore]
    public AsyncObservable<string> StderrStream { get; }

    public void UpdateStrings()
    {
        Stdout = StdoutBuilder.ToString();
        Stderr = StderrBuilder.ToString();
    }
}
