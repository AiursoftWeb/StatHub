namespace Aiursoft.StatHub.SDK.Models;

public class UfwStatus
{
    public bool IsEnabled { get; set; }
    public List<string> OpenPorts { get; set; } = [];
    public string RawOutput { get; set; } = string.Empty;
}
