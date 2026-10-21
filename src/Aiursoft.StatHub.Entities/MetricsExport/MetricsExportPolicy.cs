using System.ComponentModel.DataAnnotations;

namespace Aiursoft.StatHub.Entities.MetricsExport;

public class MetricsExportPolicy
{
    public const int DefaultId = 1;
    public int Id { get; set; } = DefaultId;
    public bool IsEnabled { get; set; } = true;
    public bool ExportAllServers { get; set; } = true;
    [ConcurrencyCheck] public Guid Version { get; set; } = Guid.NewGuid();
    public List<MetricsExportTarget> Targets { get; set; } = [];
}

public class MetricsExportTarget
{
    public int PolicyId { get; set; } = MetricsExportPolicy.DefaultId;
    public MetricsExportPolicy Policy { get; set; } = null!;
    [MaxLength(200)] public string ClientId { get; set; } = string.Empty;
    public bool IsExcluded { get; set; }
}
