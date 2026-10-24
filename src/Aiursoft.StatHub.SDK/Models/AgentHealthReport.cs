namespace Aiursoft.StatHub.SDK.Models;

/// <summary>
/// 封装所有 Agent 健康状态的计算逻辑和表示逻辑。
/// </summary>
public record AgentHealthReport
{
    // 1. 核心计算指标
    public double? SmoothedIdle { get; init; }
    public PressureInfo? Pressure { get; init; }
    public double LoadRate { get; init; }
    public int CpuRate { get; init; }
    public double DiskUseRatio { get; init; }
    public AgentStatus Status { get; init; }
    public required string Reason { get; init; }

    // 2. Tooltips (用于主页)
    public required string LoadPrompt { get; init; }
    public required string CpuPrompt { get; init; }
    public required string DiskPrompt { get; init; }

    // 3. 模板样式 (用于主页进度条)
    // Utilization and normalized load are diagnostic values, not independent health colors.
    public string LoadColorClass => "bg-info";
    public string CpuColorClass => "bg-info";

    public string DiskColorClass => DiskUseRatio < 0.8 ? "bg-success" :
        DiskUseRatio < 0.92 ? "bg-warning" : "bg-danger";

    // 4. 模板样式 (用于详情页卡片 和 主页状态图标)
    public string BadgeClass => Status switch
    {
        AgentStatus.Offline or AgentStatus.Unknown => "badge-subtle-secondary",
        AgentStatus.Critical => "badge-subtle-danger",
        AgentStatus.Warning => "badge-subtle-warning",
        _ => "badge-subtle-success"
    };

    public string BadgeText => Status == AgentStatus.Unknown ? "Collecting data" : Status.ToString();

    public string IconClass => Status switch
    {
        AgentStatus.Offline or AgentStatus.Unknown => "text-muted",
        AgentStatus.Critical => "text-danger",
        AgentStatus.Warning => "text-warning",
        _ => "text-success"
    };

    public string DetailsIconDataLucide => Status switch
    {
        AgentStatus.Offline => "server-off",
        AgentStatus.Unknown => "timer",
        AgentStatus.Critical => "alert-triangle",
        AgentStatus.Warning => "alert-circle",
        _ => "server"
    };

    public string IndexIconDataLucide => Status switch
    {
        AgentStatus.Offline or AgentStatus.Unknown => "activity",
        AgentStatus.Critical => "alert-triangle",
        AgentStatus.Warning => "alert-circle",
        _ => "check-circle"
    };
}