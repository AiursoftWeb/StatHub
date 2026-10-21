using Aiursoft.StatHub.Authorization;
using Aiursoft.StatHub.Data;
using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.Entities.MetricsExport;
using Aiursoft.StatHub.Models.MetricsExport;
using Aiursoft.StatHub.Services;
using Aiursoft.StatHub.Services.MetricsExport;
using Aiursoft.StatHub.Services.Monitoring;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Aiursoft.StatHub.Controllers;

[Authorize(Policy = AppPermissionNames.CanManageGlobalSettings)]
[LimitPerMin]
public sealed class MetricsExportController(StatHubDbContext db, MetricsExportPolicyService policies,
    MetricsExportPolicyCoordinator coordinator, MetricsExportBuffer buffer, MonitoringCoordinator monitoring,
    InMemoryDatabase agents, IOptions<MetricsExportOptions> options,
    IStringLocalizer<MetricsExportController> localizer) : Controller
{
    [RenderInNavBar(NavGroupName = "Features", NavGroupOrder = 1, CascadedLinksGroupName = "Monitoring",
        CascadedLinksIcon = "bell", CascadedLinksOrder = 2, LinkText = "ClickHouse export", LinkOrder = 2)]
    public async Task<IActionResult> Index()
    {
        await coordinator.Gate.WaitAsync();
        try
        {
            var policy = await policies.PolicyAsync();
            return this.StackView(new MetricsExportViewModel
            {
                Policy = policy,
                ConnectionEnabled = options.Value.Enabled,
                Agents = Servers(policy)
            });
        }
        finally { coordinator.Gate.Release(); }
    }

    private List<AgentMonitoringState> Servers(MetricsExportPolicy policy)
    {
        var states = monitoring.Snapshot();
        var known = states.Select(state => state.ClientId).ToHashSet(StringComparer.Ordinal);
        states.AddRange(agents.GetAgents().Where(agent => known.Add(agent.ClientId)).Select(agent =>
            new AgentMonitoringState { ClientId = agent.ClientId, Hostname = agent.Hostname, LastReportedAtUtc = agent.LastUpdate }));
        states.AddRange(policy.Targets.Where(target => known.Add(target.ClientId)).Select(target =>
            new AgentMonitoringState { ClientId = target.ClientId, Hostname = target.ClientId }));
        return states.OrderBy(state => state.Hostname).ToList();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePolicy(MetricsExportPolicyInput input)
    {
        if (!ModelState.IsValid) return BadRequest();
        await coordinator.Gate.WaitAsync();
        try
        {
            var policy = await policies.PolicyAsync();
            if (policy.Version != input.Version) return Conflict(localizer["The export policy changed. Refresh before saving."].Value);
            var known = Servers(policy).Select(state => state.ClientId).ToHashSet(StringComparer.Ordinal);
            if (input.Included.Concat(input.Excluded).Any(id => !known.Contains(id)))
                return BadRequest(localizer["Select only known servers."].Value);
            var selections = input.Included.Distinct().ToDictionary(id => id, _ => false);
            foreach (var id in input.Excluded) selections[id] = true;
            foreach (var target in policy.Targets.ToList())
            {
                if (!selections.TryGetValue(target.ClientId, out var excluded))
                {
                    db.MetricsExportTargets.Remove(target);
                    policy.Targets.Remove(target);
                }
                else { target.IsExcluded = excluded; selections.Remove(target.ClientId); }
            }
            foreach (var (id, excluded) in selections)
                policy.Targets.Add(new MetricsExportTarget { ClientId = id, IsExcluded = excluded });
            policy.IsEnabled = input.IsEnabled;
            policy.ExportAllServers = input.ExportAllServers;
            policy.Version = Guid.NewGuid();
            await db.SaveChangesAsync();
            await buffer.ApplyPolicyAsync(policy);
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(); }
        finally { coordinator.Gate.Release(); }
    }
}
