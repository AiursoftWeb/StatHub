using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Tests.UnitTests;

[TestClass]
public class AgentTests
{
    [TestMethod]
    [DataRow("127.0.0.1", true)]
    [DataRow("::1", true)]
    [DataRow("10.0.0.1", true)]
    [DataRow("172.16.0.1", true)]
    [DataRow("172.31.255.255", true)]
    [DataRow("192.168.1.1", true)]
    [DataRow("fe80::1", true)]
    [DataRow("fc00::1", true)]
    [DataRow("8.8.8.8", false)]
    [DataRow("1.1.1.1", false)]
    [DataRow("172.32.0.1", false)]
    [DataRow("192.169.1.1", false)]
    [DataRow("2001:4860:4860::8888", false)]
    [DataRow("", true)]
    [DataRow(null, true)]
    public void TestIsPrivateIp(string ip, bool expected)
    {
        var agent = new Agent("test-client") { Ip = ip };
        Assert.AreEqual(expected, agent.IsPrivateIp());
    }

    [TestMethod]
    public async Task OldAgentsAreUnconfirmedAndStillShowDiagnosticMetrics()
    {
        var agent = new Agent("old") { CpuCores = 4, TotalRoot = 100, UsedRoot = 10 };
        await agent.Stats.BroadcastAsync(new DstatResult { CpuIdl = 98, Load15M = 0.3 });
        var report = agent.GetHealthReport();
        Assert.AreEqual(AgentStatus.Unknown, report.Status);
        Assert.AreEqual(7.5, report.LoadRate);
        Assert.AreEqual(2, report.CpuRate);
        Assert.AreEqual(0.1, report.DiskUseRatio);
        Assert.AreEqual("bg-info", report.LoadColorClass);
        Assert.AreEqual("bg-info", report.CpuColorClass);
        Assert.AreEqual("badge-subtle-secondary", report.BadgeClass);
        agent.LastUpdate = DateTime.UtcNow.AddMinutes(-2);
        Assert.AreEqual(AgentStatus.Offline, agent.GetHealthReport().Status);
    }

    [TestMethod]
    public void TestAgentDefaults()
    {
        var agent = new Agent("test-client");
        Assert.AreEqual("Unknown", agent.Hostname);
        Assert.AreEqual("Unknown", agent.OsName);
        Assert.AreEqual("Unknown", agent.Ip);
        Assert.AreEqual("Unknown", agent.Version);
        Assert.AreEqual("Unknown", agent.Process);
    }
}
