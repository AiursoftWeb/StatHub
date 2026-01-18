using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Tests.UnitTests;

[TestClass]
public class DstatResultTests
{
    [TestMethod]
    public void TestDstatResultConstructor()
    {
        const string line = " 1  2 93  4  0  274M 3073M 8224k  530M   10k   20M 224B  532B 0.10 0.05 0.01";
        var result = new DstatResult(line);
        
        Assert.AreEqual(1, result.CpuUsr);
        Assert.AreEqual(2, result.CpuSys);
        Assert.AreEqual(93, result.CpuIdl);
        Assert.AreEqual(4, result.CpuWai);
        Assert.AreEqual(0, result.CpuStl);
        
        Assert.AreEqual(274 * 1024 * 1024L, result.MemUsed);
        Assert.AreEqual(3073 * 1024 * 1024L, result.MemFree);
        Assert.AreEqual(8224 * 1024L, result.MemBuf);
        Assert.AreEqual(530 * 1024 * 1024L, result.MemCach);
        
        Assert.AreEqual(10 * 1024L, result.DskRead);
        Assert.AreEqual(20 * 1024 * 1024L, result.DskWrit);
        
        Assert.AreEqual(224L, result.NetRecv);
        Assert.AreEqual(532L, result.NetSend);
        
        Assert.AreEqual(0.10, result.Load1M);
        Assert.AreEqual(0.05, result.Load5M);
        Assert.AreEqual(0.01, result.Load15M);
    }
}
