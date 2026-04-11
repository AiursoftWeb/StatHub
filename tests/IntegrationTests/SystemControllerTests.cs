using System.Net;

namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public class SystemControllerTests : TestBase
{
    [TestMethod]
    public async Task TestIndex()
    {
        // 1. Unauthorized
        var response = await Http.GetAsync("/System/Index");
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

        // 2. Logged in, no permission
        await RegisterAndLoginAsync();
        var forbiddenResponse = await Http.GetAsync("/System/Index");
        Assert.IsTrue(forbiddenResponse.StatusCode == HttpStatusCode.Forbidden || forbiddenResponse.StatusCode == HttpStatusCode.Found);

        // 3. Admin
        await LoginAsAdmin();
        var adminResponse = await Http.GetAsync("/System/Index");
        adminResponse.EnsureSuccessStatusCode();
        var html = await adminResponse.Content.ReadAsStringAsync();
        Assert.Contains("System Information", html);
    }
}
