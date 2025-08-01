using Aiursoft.AiurProtocol.Models;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.AiurProtocol.Server.Attributes;
using Aiursoft.StatHub.SDK.AddressModels;
using Aiursoft.StatHub.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Server.Controllers;

[AllowAnonymous]
[Route("api")]
[ApiModelStateChecker]
[ApiExceptionHandler(
    PassthroughRemoteErrors = true,
    PassthroughAiurServerException = true)]
public class ApiController(
    InMemoryDatabase database,
    ILogger<ApiController> logger)
    : ControllerBase
{
    [HttpGet("info")]
    public IActionResult Info()
    {
        return this.Protocol(Code.ResultShown, $"Welcome to this StatHub server!");
    }

    [HttpPost("metrics")]
    public async Task<IActionResult> Metrics([FromBody] MetricsAddressModel model)
    {
        logger.LogInformation("Received metrics from {Identity}.", model.ClientId);

        var entity = database.GetOrAddClient(model.ClientId!);
        entity.ClientId = model.ClientId!;
        entity.BootTime = model.BootTime;
        entity.Hostname = model.Hostname!;
        entity.Ip = HttpContext.Connection.RemoteIpAddress?.ToString()!;
        entity.LastUpdate = DateTime.UtcNow;
        entity.Version = model.Version!;
        entity.Process = model.Process!;
        entity.OsName = model.OsName!;
        entity.CpuCores = model.CpuCores;
        entity.RamInGb = model.RamInGb;
        entity.UsedRoot = model.UsedRoot;
        entity.TotalRoot = model.TotalRoot;
        entity.Motd = model.Motd!;
        foreach (var stat in model.Stats ?? [])
        {
            await entity.Stats.BroadcastAsync(stat);
        }
        return this.Protocol(Code.JobDone, $"Got!");
    }

    [HttpGet("clients")]
    public IActionResult Clients()
    {
        var servers = database.GetClients().ToList();
        return this.Protocol(Code.ResultShown, $"Successfully get all servers.", servers);
    }
}
