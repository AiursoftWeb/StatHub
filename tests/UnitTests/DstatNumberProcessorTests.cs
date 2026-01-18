using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Tests.UnitTests;

[TestClass]
public class DstatNumberProcessorTests
{
    [TestMethod]
    [DataRow("100", 100)]
    [DataRow("100B", 100)]
    [DataRow("1k", 1024)]
    [DataRow("1.5k", 1536)]
    [DataRow("1M", 1024 * 1024)]
    [DataRow("1.5M", 1572864)]
    [DataRow("1G", 1024L * 1024 * 1024)]
    [DataRow("1.5G", 1610612736L)]
    public void TestParseDataSize(string input, long expected)
    {
        var result = DstatNumberProcessor.ParseDataSize(input);
        Assert.AreEqual(expected, result);
    }
}
