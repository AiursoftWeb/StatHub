using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Aiursoft.StatHub.SDK.AddressModels;

public class MetricsAddressModel
{
    [Required]
    [JsonPropertyName("uptime")]
    public int UpTime { get; set; }
}