using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Server.Controllers;

[Route("api")]
public class ApiController : ControllerBase
{
    private readonly InMemoryDatabase _database;
    private readonly ILogger<ApiController> _logger;

    public ApiController(
        InMemoryDatabase database,
        ILogger<ApiController> logger)
    {
        _database = database;
        _logger = logger;
    }
    
    [HttpGet("info")]
    public IActionResult Info()
    {
        return this.Protocol(Code.ResultShown, $"Welcome to this StatHub server!");
    }
    
    [HttpPost("metrics")]
    public IActionResult Metrics([FromBody] MetricsAddressModel model)
    {
        var identity = $"{model.Hostname}-{HttpContext.Connection.RemoteIpAddress}";
        _logger.LogInformation("Received metrics from {Identity}.", identity);
        
        var entity = _database.GetOrAddClient(identity);
        entity.CpuUsage = model.CpuUsage;
        entity.BootTime = model.BootTime;
        entity.Hostname = model.Hostname ?? throw new ArgumentNullException(nameof(model.Hostname));
        entity.Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? throw new ArgumentNullException(nameof(HttpContext.Connection.RemoteIpAddress));
        entity.LastUpdate = DateTime.UtcNow;
        entity.Version = model.Version ?? throw new ArgumentNullException(nameof(model.Version));
        entity.Process = model.Process ?? throw new ArgumentNullException(nameof(model.Process));
        return this.Protocol(Code.JobDone, $"Received metrics!");
    }
    
    [HttpGet("clients")]
    public IActionResult Clients()
    {
        var servers = _database.GetClients().ToList();
        return this.Protocol(Code.ResultShown, $"Successfully get all servers.", servers);
    }
}
