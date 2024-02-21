using Aiursoft.AiurObserver.WebSocket.Server;
using Aiursoft.StatHub.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.AiurObserver.Extensions;

namespace Aiursoft.StatHub.Server.Controllers;

[Route("metrics")]
public class MetricsController(
    InMemoryDatabase database) : ControllerBase
{
    [Route("{id}/cpu.ws")]
    public async Task Cpu([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        var pusher = await HttpContext.AcceptWebSocketClient();
        var outSub = client.CpuIdl
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