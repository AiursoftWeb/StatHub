using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Server;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Server.Controllers;

[Route("api")]
public class ApiController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult Info()
    {
        return this.Protocol(Code.ResultShown, $"Welcome to this StatHub server!");
    }
}
