using System.Net;
using Aiursoft.StatHub.Configuration;
using Aiursoft.StatHub.Services;

namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public class GlobalSettingsControllerTests : TestBase
{
    [TestMethod]
    public async Task TestGlobalSettingsWorkflow()
    {
        // 1. Unauthorized
        var response = await Http.GetAsync("/GlobalSettings/Index");
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

        // 2. Admin access
        await LoginAsAdmin();
        var adminResponse = await Http.GetAsync("/GlobalSettings/Index");
        adminResponse.EnsureSuccessStatusCode();
        var html = await adminResponse.Content.ReadAsStringAsync();
        Assert.Contains("Global Settings", html);
        Assert.Contains(SettingsMap.AllowUserAdjustNickname, html);

        // 3. Edit setting
        var editResponse = await PostForm("/GlobalSettings/Edit", new Dictionary<string, string>
        {
            { "Key", SettingsMap.AllowUserAdjustNickname },
            { "Value", "False" }
        });
        AssertRedirect(editResponse, "/GlobalSettings");

        // Verify setting was updated
        using (var scope = Server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            var value = await settingsService.GetBoolSettingAsync(SettingsMap.AllowUserAdjustNickname);
            Assert.IsFalse(value);
        }

        // 4. Edit setting back
        var editResponse2 = await PostForm("/GlobalSettings/Edit", new Dictionary<string, string>
        {
            { "Key", SettingsMap.AllowUserAdjustNickname },
            { "Value", "True" }
        });
        AssertRedirect(editResponse2, "/GlobalSettings");
        
        using (var scope = Server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            var value = await settingsService.GetBoolSettingAsync(SettingsMap.AllowUserAdjustNickname);
            Assert.IsTrue(value);
        }
    }
}
