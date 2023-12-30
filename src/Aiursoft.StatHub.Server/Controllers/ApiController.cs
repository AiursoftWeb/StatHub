using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.StatHub.SDK.AddressModels;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Server.Controllers;

[Route("api")]
public class ApiController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;

    public ApiController(ILogger<ApiController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet("info")]
    public IActionResult Info()
    {
        return this.Protocol(Code.ResultShown, $"Welcome to this StatHub server!");
    }
    
    [HttpPost("metrics")]
    public async Task<IActionResult> Metrics([FromBody] MetricsAddressModel model)
    {
        _logger.LogInformation($"Received metrics: {model.UpTime}!");
        await Task.Delay(0);
        return this.Protocol(Code.JobDone, $"Received metrics!");
    }
}
