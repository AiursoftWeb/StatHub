using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Aiursoft.StatHub.SDK.AddressModels;

public class MetricsAddressModel
{
    [Required]
    [JsonPropertyName("uptime")]
    public int UpTime { get; init; }

    [Required]
    [JsonPropertyName("hostname")]
    [MaxLength(100)]
    [MinLength(1)]
    [RegularExpression("^[a-zA-Z0-9-]*$")]
    public string? Hostname { get; init; }

    [Required]
    [JsonPropertyName("cpuUsage")]
    [Range(0, 100)]
    public int CpuUsage { get; set; }

    [Required]
    [JsonPropertyName("version")]
    [MaxLength(100)]
    [MinLength(1)]
    public string? Version { get; init; }
}