using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.SDK.Models;
using Aiursoft.StatHub.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Server.Controllers;

[Route("api")]
[ApiModelStateChecker]
[ApiExceptionHandler(
    PassthroughRemoteErrors = true, 
    PassthroughAiurServerException = true)]
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
    public async Task<IActionResult> Metrics([FromBody] MetricsAddressModel model)
    {
        var identity = $"{model.Hostname}-{HttpContext.Connection.RemoteIpAddress}";
        _logger.LogInformation("Received metrics from {Identity}.", identity);
        
        var entity = _database.GetOrAddClient(identity);
        entity.BootTime = model.BootTime;
        entity.Hostname = model.Hostname ?? throw new ArgumentNullException(nameof(model.Hostname));
        entity.Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? throw new ArgumentNullException(nameof(HttpContext.Connection.RemoteIpAddress));
        entity.LastUpdate = DateTime.UtcNow;
        entity.Version = model.Version ?? throw new ArgumentNullException(nameof(model.Version));
        entity.Process = model.Process ?? throw new ArgumentNullException(nameof(model.Process));
        entity.OsName = model.OsName ?? throw new ArgumentNullException(nameof(model.OsName));
        entity.CpuCores = model.CpuCores;
        entity.RamInGb = model.RamInGb;
        entity.UsedRoot = model.UsedRoot;
        entity.TotalRoot = model.TotalRoot;
        foreach (var stat in model.Stats ?? Array.Empty<DstatResult>())
        {
            await entity.Stats.BroadcastAsync(stat);
        }
        return this.Protocol(Code.JobDone, $"Got!");
    }
    
    [HttpGet("clients")]
    public IActionResult Clients()
    {
        var servers = _database.GetClients().ToList();
        return this.Protocol(Code.ResultShown, $"Successfully get all servers.", servers);
    }
}
