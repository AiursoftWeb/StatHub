using Aiursoft.StatHub.Authorization;
using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.Entities.Monitoring;
using Aiursoft.StatHub.Models.Monitoring;
using Aiursoft.StatHub.Services;
using Aiursoft.StatHub.Services.Monitoring;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Aiursoft.StatHub.Controllers;

[Authorize(Policy = AppPermissionNames.CanManageAlertCenterIntegration)]
[LimitPerMin]
public sealed class AlertMonitoringController(StatHubDbContext db, MonitoringService monitoring,
    MonitoringCoordinator coordinator, IStringLocalizer<AlertMonitoringController> localizer) : Controller
{
    [RenderInNavBar(NavGroupName = "Features", NavGroupOrder = 1, CascadedLinksGroupName = "Monitoring",
        CascadedLinksIcon = "bell", CascadedLinksOrder = 2, LinkText = "AlertCenter monitoring", LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        await coordinator.Gate.WaitAsync();
        try
        {
            return this.StackView(new MonitoringViewModel
            {
                Policy = await monitoring.PolicyAsync(),
                Agents = await db.AgentMonitoringStates.AsNoTracking().OrderBy(item => item.Hostname).ToListAsync()
            });
        }
        finally { coordinator.Gate.Release(); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePolicy(MonitoringPolicyInput input)
    {
        if (!ModelState.IsValid) return BadRequest();
        await coordinator.Gate.WaitAsync();
        try
        {
            var policy = await monitoring.PolicyAsync();
            if (policy.Version != input.Version) return Conflict(localizer["The monitoring policy changed. Refresh before saving."].Value);
            var known = await db.AgentMonitoringStates.Select(item => item.ClientId).ToListAsync();
            if (input.Included.Concat(input.Excluded).Any(id => !known.Contains(id)))
                return BadRequest(localizer["Select only known servers."].Value);
            var selections = input.Included.Distinct().ToDictionary(id => id, _ => false);
            foreach (var id in input.Excluded) selections[id] = true;
            foreach (var target in policy.Targets.ToList())
            {
                if (!selections.TryGetValue(target.ClientId, out var excluded)) db.AlertMonitoringTargets.Remove(target);
                else { target.IsExcluded = excluded; selections.Remove(target.ClientId); }
            }
            foreach (var (id, excluded) in selections)
                policy.Targets.Add(new MonitoringTarget { ClientId = id, IsExcluded = excluded });
            policy.IsEnabled = input.IsEnabled;
            policy.MonitorAllServers = input.MonitorAllServers;
            policy.AutoResolveOnGreen = input.AutoResolveOnGreen;
            policy.Version = Guid.NewGuid();
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(); }
        finally { coordinator.Gate.Release(); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRule(Guid version, Guid? id, string state, int durationMinutes, string severity, bool isEnabled)
    {
        if (!ModelState.IsValid || state is not ("yellow" or "red" or "grey") || durationMinutes is < 0 or > 525600 ||
            severity is not ("sev0" or "sev1" or "sev2" or "sev3" or "sev4"))
            return BadRequest(localizer["Select a valid state, severity and duration between zero and 525600 minutes."].Value);
        await coordinator.Gate.WaitAsync();
        try
        {
            var policy = await monitoring.PolicyAsync();
            if (policy.Version != version) return Conflict();
            var rule = id.HasValue ? policy.Rules.SingleOrDefault(item => item.Id == id) : new MonitoringRule();
            if (rule == null) return NotFound();
            if (!id.HasValue)
            {
                rule.Policy = policy;
                db.AlertMonitoringRules.Add(rule);
            }
            rule.State = state; rule.DurationMinutes = durationMinutes; rule.Severity = severity; rule.IsEnabled = isEnabled;
            policy.Version = Guid.NewGuid();
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(); }
        finally { coordinator.Gate.Release(); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRule(Guid version, Guid id)
    {
        await coordinator.Gate.WaitAsync();
        try
        {
            var policy = await monitoring.PolicyAsync();
            if (policy.Version != version) return Conflict();
            var rule = policy.Rules.SingleOrDefault(item => item.Id == id);
            if (rule == null) return NotFound();
            db.AlertMonitoringRules.Remove(rule);
            policy.Version = Guid.NewGuid();
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(); }
        finally { coordinator.Gate.Release(); }
    }
}
