using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aiursoft.StatHub.SDK.Models;

public class JsonRpcMessage
{
    [JsonProperty("jsonrpc")]
    public string Jsonrpc { get; set; } = "2.0";

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("method")]
    public string Method { get; set; } = string.Empty;

    [JsonProperty("params")]
    public JToken? Params { get; set; }
}
