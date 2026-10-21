namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public class HomeControllerTests : TestBase
{
    [TestMethod]
    public async Task GetIndex()
    {
        var url = "/";
        var response = await Http.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task GetSelfHost()
    {
        var url = "/Home/SelfHost";
        var response = await Http.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task GetBuildDashboard()
    {
        var response = await Http.GetAsync("/Home/BuildDashboard");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(html, "StatHubMetrics.ServerMetricsLatest");
        StringAssert.Contains(html, "MetricsExport__Enabled=true");
        StringAssert.Contains(html, "/images/grafana-dashboard.png");
        StringAssert.Contains(html, "/Home/SelfHost");
    }

}
