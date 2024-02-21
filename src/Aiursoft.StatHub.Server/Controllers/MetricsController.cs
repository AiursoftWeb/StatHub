using Aiursoft.AiurObserver;
using Aiursoft.AiurObserver.WebSocket.Server;
using Aiursoft.StatHub.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.AiurObserver.Extensions;
using Aiursoft.WebTools.Attributes;

namespace Aiursoft.StatHub.Server.Controllers;

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
            .Throttle(TimeSpan.FromSeconds(0.99)) // Smaller than 1 second to catch the latest data.
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
        finally
        {
            await pusher.Close(HttpContext.RequestAborted);
            outSub.Unsubscribe();
        }
    }
}