using System.ComponentModel.DataAnnotations;
using Aiursoft.StatHub.SDK.Models;
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

    [JsonProperty("version")]
    [MaxLength(100)]
    [MinLength(1)]
    public string? Version { get; init; } = "unknown";

    [JsonProperty("process")]
    [MaxLength(100)]
    [MinLength(1)]
    public string? Process { get; init; } = "unknown";

    [JsonProperty("stats")]
    [Required]
    [MinLength(10)]
    [MaxLength(10)]
    public DstatResult[]? Stats { get; set; }

    [JsonProperty("osName")]
    [MaxLength(100)]
    [MinLength(1)]
    public string? OsName { get; set; } = "unknown";

    [JsonProperty("ramInGb")]
    [Range(0, 8192)]
    public int RamInGb { get; set; }
    
    [JsonProperty("cpuCores")]
    [Range(0, 2048)]
    public int CpuCores { get; set; }

    [JsonProperty("usedRoot")]
    public int UsedRoot { get; set; }
    
    [JsonProperty("totalRoot")]
    public int TotalRoot { get; set; }

    [JsonProperty("clientId")]
    [Required]
    public string? ClientId { get; set; }
}