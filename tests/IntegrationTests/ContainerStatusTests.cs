using System.Net;
using Aiursoft.StatHub.Data;
using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public class ContainerStatusTests : TestBase
{
    [TestMethod]
    public async Task TestContainerStatusColors()
    {
        await LoginAsAdmin();
        var db = Server!.Services.GetRequiredService<InMemoryDatabase>();
        var clientId = Guid.NewGuid().ToString();
        var agent = db.GetOrAddClient(clientId);
        
        agent.Containers = new List<ContainerInfo>
        {
            new ContainerInfo
            {
                Id = "1",
                Name = "healthy-container",
                Status = "Up 1 hour (healthy)",
                IsHealthy = true,
                HasHealthCheck = true,
                State = "running",
                Image = "image:latest",
                Ports = "80/tcp",
                Uptime = "1 hour"
            },
            new ContainerInfo
            {
                Id = "2",
                Name = "unhealthy-container",
                Status = "Up 1 hour (unhealthy)",
                IsHealthy = false, // Unhealthy
                HasHealthCheck = true,
                State = "running",
                Image = "image:latest",
                Ports = "80/tcp",
                Uptime = "1 hour"
            },
            new ContainerInfo
            {
                Id = "3",
                Name = "normal-container",
                Status = "Up 1 hour",
                IsHealthy = true, // Running
                HasHealthCheck = false,
                State = "running",
                Image = "image:latest",
                Ports = "80/tcp",
                Uptime = "1 hour"
            }
        };

        var url = $"/Dashboard/Details/{clientId}";
        var response = await Http.GetAsync(url);
        
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        // Check for healthy container styling
        StringAssert.Contains(content, "healthy-container", "Should contain healthy-container name");
        StringAssert.Contains(content, "<span class=\"text-success small\">running (Up 1 hour (healthy))</span>", "Healthy container should have text-success class");

        // Check for unhealthy container styling
        StringAssert.Contains(content, "unhealthy-container", "Should contain unhealthy-container name");
        StringAssert.Contains(content, "<span class=\"text-danger small\">running (Up 1 hour (unhealthy))</span>", "Unhealthy container should have text-danger class");

        // Check for normal container styling
        StringAssert.Contains(content, "normal-container", "Should contain normal-container name");
        StringAssert.Contains(content, "<span class=\"text-muted small\">running (Up 1 hour)</span>", "Normal container should have text-muted class");
    }
}
