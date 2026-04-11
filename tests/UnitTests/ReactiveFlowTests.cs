using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Tests.UnitTests;

[TestClass]
public class ReactiveFlowTests
{
    [TestMethod]
    public async Task TestCommandExecutionReactiveState()
    {
        // 验证 CommandExecution 是否能正确通过管道更新自己的字符串状态
        var exec = new CommandExecution();
        
        Assert.AreEqual(string.Empty, exec.Stdout);

        // 模拟管道输入
        await exec.StdoutStream.BroadcastAsync("Hello ");
        await exec.StdoutStream.BroadcastAsync("World!");

        // 验证状态是否同步更新
        Assert.AreEqual("Hello World!", exec.Stdout);
        
        await exec.StderrStream.BroadcastAsync("Error occurred");
        Assert.AreEqual("Error occurred", exec.Stderr);
    }

    [TestMethod]
    public async Task TestAgentReactiveSkeleton()
    {
        // 验证 Agent 构造函数中建立的骨架是否有效
        var agent = new Agent("test-id");
        
        var stat = new DstatResult("1 2 97 0 0 100M 200M 0 0 0 0 0 0 0.5 0.6 0.7");
        
        // 广播到 Stats 流
        await agent.Stats.BroadcastAsync(stat);

        // 验证分流器是否工作
        Assert.AreEqual(1, agent.CpuUsr.Stage);
        Assert.AreEqual(2, agent.CpuSys.Stage);
        Assert.AreEqual(97, agent.CpuIdl.Stage);
        Assert.AreEqual(100L * 1024 * 1024, agent.MemUsed.Stage);
    }
}
