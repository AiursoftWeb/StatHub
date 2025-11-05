using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.WebSocket.Server;
using Aiursoft.StatHub.Authorization;
using Aiursoft.StatHub.Data;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Controllers;

[Authorize(Policy = AppPermissionNames.CanViewDashboard)]
[Route("metrics")]
[LimitPerMin]
public class MetricsController(
    InMemoryDatabase database) : ControllerBase
{
    private async Task StreamMetricAsync<T>(
        IAsyncObservable<T> observable,
        Func<T, string> formatter,
        TimeSpan? throttle = null)
    {
        var pusher = await HttpContext.AcceptWebSocketClient();

        var stream = observable.InNewThread()
            .Throttle(throttle ?? TimeSpan.FromSeconds(1));

        var outSub = stream
            .Map(formatter)
            .Subscribe(t => pusher.Send(t, HttpContext.RequestAborted));

        try
        {
            await pusher.Listen(HttpContext.RequestAborted);
        }
        catch (TaskCanceledException)
        {
            // 忽略。客户端关闭连接时发生。
        }
        catch (ConnectionAbortedException)
        {
            // 忽略。客户端关闭连接时发生。
        }
        finally
        {
            outSub.Unsubscribe();
            if (pusher.Connected)
            {
                await pusher.Close(HttpContext.RequestAborted);
            }
        }
    }

    [Route("{id}/cpu.ws")]
    [EnforceWebSocket]
    public async Task Cpu([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        await StreamMetricAsync(
            client.CpuIdl,
            t => (100 - t).ToString());
    }

    [Route("{id}/ram.ws")]
    [EnforceWebSocket]
    public async Task Ram([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        await StreamMetricAsync(
            client.MemUsed,
            t => t.ToString());
    }


    [Route("{id}/load1m.ws")]
    [EnforceWebSocket]
    public async Task Load1M([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        await StreamMetricAsync(
            client.Load1M,
            t => t.ToString("F2"));
    }

    [Route("{id}/load5m.ws")]
    [EnforceWebSocket]
    public async Task Load5M([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        await StreamMetricAsync(
            client.Load5M,
            t => t.ToString("F2"));
    }

    [Route("{id}/load15m.ws")]
    [EnforceWebSocket]
    public async Task Load15M([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        await StreamMetricAsync(
            client.Load15M,
            t => t.ToString("F2"));
    }


    [Route("{id}/net-recv.ws")]
    [EnforceWebSocket]
    public async Task NetRecv([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        await StreamMetricAsync(
            client.Stats.Map(s => s.NetRecv),
            t => t.ToString());
    }

    [Route("{id}/net-send.ws")]
    [EnforceWebSocket]
    public async Task NetSend([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        await StreamMetricAsync(
            client.Stats.Map(s => s.NetSend),
            t => t.ToString());
    }

    [Route("{id}/disk-read.ws")]
    [EnforceWebSocket]
    public async Task DiskRead([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        await StreamMetricAsync(
            client.Stats.Map(s => s.DskRead),
            t => t.ToString());
    }

    [Route("{id}/disk-writ.ws")]
    [EnforceWebSocket]
    public async Task DiskWrit([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        await StreamMetricAsync(
            client.Stats.Map(s => s.DskWrit),
            t => t.ToString());
    }
}
