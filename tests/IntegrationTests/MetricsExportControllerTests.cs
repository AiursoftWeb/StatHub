using System.Net;
using Aiursoft.StatHub.Data;
using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.Entities.MetricsExport;
using Aiursoft.StatHub.SDK.Models;
using Aiursoft.StatHub.Services.MetricsExport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public sealed class MetricsExportControllerTests : TestBase
{
    private async Task<MetricsExportPolicy> PolicyAsync()
    {
        using var scope = Server!.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<StatHubDbContext>().MetricsExportPolicies
            .AsNoTracking().Include(policy => policy.Targets).SingleAsync();
    }

    [TestMethod]
    public async Task ExportPageAndSaveRequireGlobalSettingsPermission()
    {
        var anonymous = await Http.GetAsync("/MetricsExport");
        Assert.AreEqual(HttpStatusCode.Redirect, anonymous.StatusCode);
        await RegisterAndLoginAsync();
        var denied = await Http.GetAsync("/MetricsExport");
        AssertRedirect(denied, "/Error/Code403", exact: false);
        var save = await PostForm("/MetricsExport/SavePolicy", new(), includeToken: false);
        AssertRedirect(save, "/Error/Code403", exact: false);
    }

    [TestMethod]
    public async Task ExclusionWinsAndSavingUpdatesPendingAndFutureExportsWithoutChangingAlerts()
    {
        await LoginAsAdmin();
        await Http.GetStringAsync("/AlertMonitoring");
        var agents = GetService<InMemoryDatabase>();
        agents.GetOrAddClient("selected").Hostname = "Selected host";
        agents.GetOrAddClient("excluded").Hostname = "Excluded host";
        await Http.GetStringAsync("/MetricsExport");
        var policy = await PolicyAsync();
        Assert.IsTrue(policy.IsEnabled && policy.ExportAllServers);
        GetService<IOptions<MetricsExportOptions>>().Value.Enabled = true;
        var buffer = GetService<MetricsExportBuffer>();
        buffer.Capture("selected", "host", new DstatResult(), DateTime.UtcNow);
        buffer.Capture("excluded", "host", new DstatResult(), DateTime.UtcNow);
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StatHubDbContext>();
        var alert = await db.AlertMonitoringPolicies.AsNoTracking().Include(item => item.Targets).SingleAsync();
        var saved = await PostForm("/MetricsExport/SavePolicy", new()
        {
            ["Version"] = policy.Version.ToString(), ["IsEnabled"] = "true", ["ExportAllServers"] = "true",
            ["Included"] = "excluded", ["Excluded"] = "excluded"
        }, "/MetricsExport");
        AssertRedirect(saved, "/MetricsExport");
        policy = await PolicyAsync();
        Assert.IsTrue(policy.Targets.Single().IsExcluded);
        Assert.AreEqual(1, buffer.Count);
        buffer.Capture("excluded", "host", new DstatResult(), DateTime.UtcNow);
        buffer.Capture("new-server", "host", new DstatResult(), DateTime.UtcNow);
        Assert.AreEqual(2, await buffer.FlushAsync(batch =>
        {
            Assert.IsFalse(batch.Any(sample => sample.ClientId == "excluded"));
            return Task.CompletedTask;
        }));
        var unchanged = await db.AlertMonitoringPolicies.AsNoTracking().Include(item => item.Targets).SingleAsync();
        Assert.AreEqual(alert.Version, unchanged.Version);
        Assert.AreEqual(alert.IsEnabled, unchanged.IsEnabled);
        Assert.AreEqual(alert.MonitorAllServers, unchanged.MonitorAllServers);
        Assert.AreEqual(alert.Targets.Count, unchanged.Targets.Count);
    }

    [TestMethod]
    public async Task SavedOfflineSelectionRemainsEditableAndReloadsBeforeNewSamples()
    {
        await LoginAsAdmin();
        var policy = await PolicyAsync();
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StatHubDbContext>();
            db.MetricsExportTargets.Add(new MetricsExportTarget { ClientId = "offline-server" });
            await db.SaveChangesAsync();
        }
        var html = await Http.GetStringAsync("/MetricsExport");
        StringAssert.Contains(html, "offline-server");
        StringAssert.Contains(html, "No report since restart");
        var save = await PostForm("/MetricsExport/SavePolicy", new()
        {
            ["Version"] = policy.Version.ToString(), ["IsEnabled"] = "true", ["Included"] = "offline-server"
        }, "/MetricsExport");
        AssertRedirect(save, "/MetricsExport");
        policy = await PolicyAsync();
        Assert.IsFalse(policy.ExportAllServers);
        Assert.IsFalse(policy.Targets.Single().IsExcluded);
        var restored = new MetricsExportBuffer(Options.Create(new MetricsExportOptions { Enabled = true }));
        await restored.ApplyPolicyAsync(policy);
        restored.Capture("new-server", "host", new DstatResult(), DateTime.UtcNow);
        restored.Capture("offline-server", "host", new DstatResult(), DateTime.UtcNow);
        Assert.AreEqual(1, await restored.FlushAsync(_ => Task.CompletedTask));
        var disable = await PostForm("/MetricsExport/SavePolicy", new()
        {
            ["Version"] = policy.Version.ToString(), ["Included"] = "offline-server"
        }, "/MetricsExport");
        AssertRedirect(disable, "/MetricsExport");
        policy = await PolicyAsync();
        Assert.IsFalse(policy.IsEnabled);
        Assert.AreEqual("offline-server", policy.Targets.Single().ClientId);
        html = await Http.GetStringAsync("/MetricsExport");
        StringAssert.Contains(html, "offline-server");
    }

    [TestMethod]
    public async Task StaleUnknownAndForgedSavesLeaveTheCurrentPolicyUnchanged()
    {
        await LoginAsAdmin();
        await Http.GetStringAsync("/MetricsExport");
        var policy = await PolicyAsync();
        var missingCsrf = await PostForm("/MetricsExport/SavePolicy", new() { ["Version"] = policy.Version.ToString() }, includeToken: false);
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        var unknown = await PostForm("/MetricsExport/SavePolicy", new()
        {
            ["Version"] = policy.Version.ToString(), ["Included"] = "unknown-server"
        }, "/MetricsExport");
        Assert.AreEqual(HttpStatusCode.BadRequest, unknown.StatusCode);
        var invalid = await PostForm("/MetricsExport/SavePolicy", new() { ["Version"] = "not-a-version" }, "/MetricsExport");
        Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
        var saved = await PostForm("/MetricsExport/SavePolicy", new()
        {
            ["Version"] = policy.Version.ToString(), ["IsEnabled"] = "true", ["ExportAllServers"] = "true"
        }, "/MetricsExport");
        AssertRedirect(saved, "/MetricsExport");
        var current = await PolicyAsync();
        var stale = await PostForm("/MetricsExport/SavePolicy", new() { ["Version"] = policy.Version.ToString() }, "/MetricsExport");
        Assert.AreEqual(HttpStatusCode.Conflict, stale.StatusCode);
        var after = await PolicyAsync();
        Assert.AreEqual(current.Version, after.Version);
        Assert.IsTrue(after.IsEnabled && after.ExportAllServers);
        Assert.AreEqual(0, after.Targets.Count);
    }

    [TestMethod]
    public async Task RemovingAnExplicitInclusionImmediatelyStopsItAndClearsPendingSamples()
    {
        await LoginAsAdmin();
        GetService<InMemoryDatabase>().GetOrAddClient("selected");
        var policy = await PolicyAsync();
        var included = await PostForm("/MetricsExport/SavePolicy", new()
        {
            ["Version"] = policy.Version.ToString(), ["IsEnabled"] = "true", ["Included"] = "selected"
        }, "/MetricsExport");
        AssertRedirect(included, "/MetricsExport");
        GetService<IOptions<MetricsExportOptions>>().Value.Enabled = true;
        var buffer = GetService<MetricsExportBuffer>();
        buffer.Capture("selected", "host", new DstatResult(), DateTime.UtcNow);
        Assert.AreEqual(1, buffer.Count);
        policy = await PolicyAsync();
        var removed = await PostForm("/MetricsExport/SavePolicy", new()
        {
            ["Version"] = policy.Version.ToString(), ["IsEnabled"] = "true"
        }, "/MetricsExport");
        AssertRedirect(removed, "/MetricsExport");
        Assert.AreEqual(0, (await PolicyAsync()).Targets.Count);
        Assert.AreEqual(0, buffer.Count);
        buffer.Capture("selected", "host", new DstatResult(), DateTime.UtcNow);
        Assert.AreEqual(0, buffer.Count);
    }

}
