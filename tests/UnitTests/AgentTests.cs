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
    public async Task TestGetHealthReport_Healthy()
    {
        var agent = new Agent("test-client")
        {
            LastUpdate = DateTime.UtcNow,
            CpuCores = 4,
            TotalRoot = 100,
            UsedRoot = 10
        };
        await agent.Stats.BroadcastAsync(new DstatResult
        {
            CpuUsr = 1,
            CpuSys = 1,
            CpuIdl = 98,
            Load15M = 0.3 // 7.5% load per core
        });

        var report = agent.GetHealthReport();
        Assert.AreEqual(AgentStatus.Healthy, report.Status);
        Assert.AreEqual(7.5, report.LoadRate);
        Assert.AreEqual(2, report.CpuRate);
        Assert.AreEqual(0.1, report.DiskUseRatio);
        Assert.AreEqual("bg-success", report.LoadColorClass);
        Assert.AreEqual("bg-success", report.CpuColorClass);
        Assert.AreEqual("bg-success", report.DiskColorClass);
    }

    [TestMethod]
    public void TestGetHealthReport_Offline()
    {
        var agent = new Agent("test-client")
        {
            LastUpdate = DateTime.UtcNow.AddMinutes(-2),
            CpuCores = 4,
            TotalRoot = 100,
            UsedRoot = 10
        };
        
        var report = agent.GetHealthReport();
        Assert.AreEqual(AgentStatus.Offline, report.Status);
    }

    [TestMethod]
    public async Task TestGetHealthReport_Critical()
    {
        var agent = new Agent("test-client")
        {
            LastUpdate = DateTime.UtcNow,
            CpuCores = 1,
            TotalRoot = 100,
            UsedRoot = 80
        };
        await agent.Stats.BroadcastAsync(new DstatResult
        {
            CpuUsr = 50,
            CpuSys = 0,
            CpuIdl = 50,
            Load15M = 1.1 
        });

        var report = agent.GetHealthReport();
        Assert.AreEqual(AgentStatus.Critical, report.Status);
        StringAssert.Contains(report.Reason, "Load critical");
        StringAssert.Contains(report.Reason, "CPU critical");
        StringAssert.Contains(report.Reason, "Disk critical");
    }
}
