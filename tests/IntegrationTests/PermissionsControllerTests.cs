using System.Net;
using Aiursoft.StatHub.Authorization;

namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public class PermissionsControllerTests : TestBase
{
    [TestMethod]
    public async Task TestIndex()
    {
        // 1. Unauthorized access (not logged in)
        var response = await Http.GetAsync("/Permissions/Index");
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode); // Redirect to login

        // 2. Logged in but no permission (default user)
        await RegisterAndLoginAsync();
        var forbiddenResponse = await Http.GetAsync("/Permissions/Index");
        // Actually Aiursoft apps usually redirect to an access denied page or home when forbidden
        // Let's see how it behaves. If it has [Authorize(Policy = ...)] it might return 403 or redirect.
        Assert.IsTrue(forbiddenResponse.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect or HttpStatusCode.Found);

        // 3. Admin access
        await LoginAsAdmin();
        var adminResponse = await Http.GetAsync("/Permissions/Index");
        adminResponse.EnsureSuccessStatusCode();
        var html = await adminResponse.Content.ReadAsStringAsync();
        Assert.Contains("Permissions", html);
        Assert.Contains(AppPermissionNames.CanReadPermissions, html);
    }

    [TestMethod]
    public async Task TestDetails()
    {
        await LoginAsAdmin();
        
        // 1. Valid permission key
        var key = AppPermissionNames.CanReadPermissions;
        var response = await Http.GetAsync($"/Permissions/Details?key={key}");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(key, html);
        Assert.Contains("Administrators", html);

        // 2. Invalid permission key
        var invalidResponse = await Http.GetAsync("/Permissions/Details?key=InvalidKey");
        Assert.AreEqual(HttpStatusCode.NotFound, invalidResponse.StatusCode);

        // 3. Null key
        var nullResponse = await Http.GetAsync("/Permissions/Details");
        Assert.AreEqual(HttpStatusCode.NotFound, nullResponse.StatusCode);
    }
}
