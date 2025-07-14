using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.WebSocket.Server;
using Aiursoft.StatHub.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;

namespace Aiursoft.StatHub.Server.Controllers;

[Authorize]
[Route("metrics")]
public class MetricsController(
    InMemoryDatabase database) : ControllerBase
{
    [Route("{id}/cpu.ws")]
    [EnforceWebSocket]
    public async Task Cpu([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        var pusher = await HttpContext.AcceptWebSocketClient();
        var outSub = client.CpuIdl
            .InNewThread()
            .Throttle(TimeSpan.FromSeconds(1))
            .Map(t => 100 - t)
            .Subscribe(t => pusher.Send(t.ToString(), HttpContext.RequestAborted));

        try
        {
            await pusher.Listen(HttpContext.RequestAborted);
        }
        catch (TaskCanceledException)
        {
            // Ignore. This happens when the client closes the connection.
        }
        catch (ConnectionAbortedException)
        {
            // Ignore. This happens when the client closes the connection.
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

    [Route("{id}/ram.ws")]
    [EnforceWebSocket]
    public async Task Ram([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        var pusher = await HttpContext.AcceptWebSocketClient();
        var outSub = client.MemUsed
            .InNewThread()
            .Map(m => m)
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(t => pusher.Send(t.ToString(), HttpContext.RequestAborted));

        try
        {
            await pusher.Listen(HttpContext.RequestAborted);
        }
        catch (TaskCanceledException)
        {
            // Ignore. This happens when the client closes the connection.
        }
        finally
        {
            await pusher.Close(HttpContext.RequestAborted);
            outSub.Unsubscribe();
        }
    }

}
