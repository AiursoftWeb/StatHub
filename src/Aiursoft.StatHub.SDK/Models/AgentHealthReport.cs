namespace Aiursoft.StatHub.SDK.Models;

/// <summary>
/// 封装所有 Agent 健康状态的计算逻辑和表示逻辑。
/// </summary>
public record AgentHealthReport
{
    // 1. 核心计算指标
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
    public string LoadColorClass => LoadRate < 10 ? "bg-success" :
        LoadRate < 15 ? "bg-info" :
        LoadRate < 70 ? "bg-secondary" :
        LoadRate < 100 ? "bg-warning" : "bg-danger";

    public string CpuColorClass => CpuRate < 5 ? "bg-success" :
        CpuRate < 10 ? "bg-info" :
        CpuRate < 20 ? "bg-secondary" :
        CpuRate < 40 ? "bg-warning" : "bg-danger";

    public string DiskColorClass => DiskUseRatio < 0.6 ? "bg-success" :
        DiskUseRatio < 0.7 ? "bg-warning" : "bg-danger";

    // 4. 模板样式 (用于详情页卡片 和 主页状态图标)
    public string BadgeClass => Status switch
    {
        AgentStatus.Offline => "badge-subtle-secondary",
        AgentStatus.Critical => "badge-subtle-danger",
        AgentStatus.Warning => "badge-subtle-warning",
        _ => "badge-subtle-success"
    };

    public string BadgeText => Status.ToString();

    public string IconClass => Status switch
    {
        AgentStatus.Offline => "text-muted",
        AgentStatus.Critical => "text-danger",
        AgentStatus.Warning => "text-warning",
        _ => "text-success"
    };

    public string DetailsIconDataLucide => Status switch
    {
        AgentStatus.Offline => "server-off",
        AgentStatus.Critical => "alert-triangle",
        AgentStatus.Warning => "alert-circle",
        _ => "server"
    };

    public string IndexIconDataLucide => Status switch
    {
        AgentStatus.Offline => "activity",
        AgentStatus.Critical => "alert-triangle",
        AgentStatus.Warning => "alert-circle",
        _ => "check-circle"
    };
}