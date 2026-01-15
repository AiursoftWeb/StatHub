using Newtonsoft.Json;

namespace Aiursoft.StatHub.Services;

public class IpApiResponse
{
    [JsonProperty("ip")]
    public string? Ip { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }
}