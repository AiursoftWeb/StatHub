using System.Net;
using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.Entities.Monitoring;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public sealed class MonitoringControllerTests : TestBase
{
    [TestMethod]
    public async Task ConfiguredExclusionsRemainVisibleAndEditableBeforeServersReportAfterRestart()
    {
        await LoginAsAdmin();
        await Http.GetStringAsync("/AlertMonitoring");
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StatHubDbContext>();
        var policy = await db.AlertMonitoringPolicies.SingleAsync();
        db.AlertMonitoringTargets.Add(new MonitoringTarget
        {
            PolicyId = policy.Id, ClientId = "offline-excluded-server", IsExcluded = true
        });
        await db.SaveChangesAsync();
        var html = await Http.GetStringAsync("/AlertMonitoring");
        Assert.Contains("offline-excluded-server", html);
        Assert.Contains("No report since restart", html);
        using var saved = await PostForm("/AlertMonitoring/SavePolicy", new()
        {
            ["Version"] = policy.Version.ToString(), ["MonitorAllServers"] = "true",
            ["IsEnabled"] = "true", ["AutoResolveOnGreen"] = "true", ["Excluded"] = "offline-excluded-server"
        }, "/AlertMonitoring");
        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.IsTrue(await db.AlertMonitoringTargets.AnyAsync(item => item.ClientId == "offline-excluded-server" && item.IsExcluded));
    }

    [TestMethod]
    public async Task MonitoringPageRequiresItsPermission()
    {
        await RegisterAndLoginAsync();
        var denied = await Http.GetAsync("/AlertMonitoring");
        Assert.AreEqual(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("Code403", denied.Headers.Location!.ToString());
    }

    [TestMethod]
    public async Task AdministratorCanEditRulesAndRemoveAllWithoutReseeding()
    {
        await LoginAsAdmin();
        var html = await Http.GetStringAsync("/AlertMonitoring");
        Assert.Contains("Continuous minutes", html);
        Assert.Contains("Grey (offline)", html);
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StatHubDbContext>();
        var policy = await db.AlertMonitoringPolicies.AsNoTracking().SingleAsync();
        using var missingCsrf = await PostForm("/AlertMonitoring/SavePolicy", new() { ["Version"] = policy.Version.ToString() }, includeToken: false);
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using var saved = await PostForm("/AlertMonitoring/SaveRule", new()
        {
            ["version"] = policy.Version.ToString(), ["state"] = "grey", ["durationMinutes"] = "0", ["severity"] = "sev1", ["isEnabled"] = "true"
        }, "/AlertMonitoring");
        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.AreEqual(7, await db.AlertMonitoringRules.CountAsync());
        using var stale = await PostForm("/AlertMonitoring/SavePolicy", new() { ["Version"] = policy.Version.ToString() }, "/AlertMonitoring");
        Assert.AreEqual(HttpStatusCode.Conflict, stale.StatusCode);
        foreach (var id in await db.AlertMonitoringRules.Select(item => item.Id).ToListAsync())
        {
            policy = await db.AlertMonitoringPolicies.AsNoTracking().SingleAsync();
            using var deleted = await PostForm("/AlertMonitoring/DeleteRule", new() { ["version"] = policy.Version.ToString(), ["id"] = id.ToString() }, "/AlertMonitoring");
            Assert.AreEqual(HttpStatusCode.Redirect, deleted.StatusCode);
        }
        await Http.GetStringAsync("/AlertMonitoring");
        Assert.AreEqual(0, await db.AlertMonitoringRules.CountAsync());
    }
}
