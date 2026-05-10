using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Tests.UnitTests;

[TestClass]
public class DockerNumberProcessorTests
{
    [TestMethod]
    [DataRow("100B", 100)]
    [DataRow("100 B", 100)]
    [DataRow("1KiB", 1024)]
    [DataRow("1.5MiB", 1572864)]
    [DataRow("1GiB", 1024L * 1024 * 1024)]
    [DataRow("1 TiB", 1024L * 1024 * 1024 * 1024)]
    [DataRow("1KB", 1000)]
    [DataRow("1MB", 1000 * 1000)]
    [DataRow("1GB", 1000 * 1000 * 1000)]
    [DataRow("123", 123)]
    [DataRow("1.1MB / 2.2MB", 1100000)] // Should take the first part
    [DataRow("", 0)]
    [DataRow(null, 0)]
    [DataRow("Invalid", 0)]
    public void TestParseDockerSize(string input, long expected)
    {
        var result = DockerNumberProcessor.ParseDockerSize(input);
        Assert.AreEqual(expected, result);
    }
}
