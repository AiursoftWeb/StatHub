using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Aiursoft.StatHub.SDK.AddressModels;

public class MetricsAddressModel
{
    [Required]
    [JsonProperty("bootTime")]
    public DateTime BootTime { get; init; }

    [Required]
    [JsonProperty("hostname")]
    [MaxLength(100)]
    [MinLength(1)]
    [RegularExpression("^[a-zA-Z0-9-]*$")]
    public string? Hostname { get; init; }

    [Required]
    [JsonProperty("cpuUsage")]
    [Range(0, 100)]
    public int CpuUsage { get; set; }

    [JsonProperty("version")]
    [MaxLength(100)]
    [MinLength(1)]
    public string? Version { get; init; } = "unknown";

    [JsonProperty("process")]
    [MaxLength(100)]
    [MinLength(1)]
    public string? Process { get; init; } = "unknown";
}