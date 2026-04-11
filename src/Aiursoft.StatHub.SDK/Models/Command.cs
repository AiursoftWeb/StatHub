using Newtonsoft.Json;

namespace Aiursoft.StatHub.SDK.Models;

public class Command
{
    [JsonProperty("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("issuedAt")]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
