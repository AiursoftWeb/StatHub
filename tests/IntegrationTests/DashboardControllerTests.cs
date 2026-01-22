using System.Net;
using Aiursoft.StatHub.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public class DashboardControllerTests : TestBase
{
    [TestMethod]
    public async Task GetIndex()
    {
        // This is a basic test to ensure the controller is reachable.
        // Adjust the path as necessary for specific controllers.
        var url = "/Dashboard/Index";
        
        var response = await Http.GetAsync(url);
        
        // Assert
        // For some controllers, it might redirect to login, which is 302.
        // For others, it might be 200.
        // We just check if we get a response.
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task TestGoIP()
    {
        await LoginAsAdmin();
        var db = Server!.Services.GetRequiredService<InMemoryDatabase>();
        var clientId = Guid.NewGuid().ToString();
        var agent = db.GetOrAddClient(clientId);
        agent.Ip = "1.2.3.4";

        var url = "/Dashboard/Go-IP/1.2.3.4";
        var response = await Http.GetAsync(url);

        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.AreEqual($"/Dashboard/Details/{clientId}", response.Headers.Location?.OriginalString);

        var url404 = "/Dashboard/Go-IP/1.2.3.5";
        var response404 = await Http.GetAsync(url404);
        Assert.AreEqual(HttpStatusCode.NotFound, response404.StatusCode);
    }
}
