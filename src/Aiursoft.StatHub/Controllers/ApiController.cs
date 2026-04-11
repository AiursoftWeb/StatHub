using Aiursoft.AiurProtocol.Models;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.AiurProtocol.Server.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Controllers;

[AllowAnonymous]
[Route("api")]
[ApiModelStateChecker]
[ApiExceptionHandler(
    PassthroughRemoteErrors = true,
    PassthroughAiurServerException = true)]
public class ApiController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult Info()
    {
        return this.Protocol(Code.ResultShown, $"Welcome to this StatHub server!");
    }
}
