﻿using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class OsInfoService
{
    private readonly CacheService _cacheService;

    public OsInfoService(
        CacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task<string> GetOsInfoAsync()
    {
        return _cacheService.RunWithCache("os-info", async () =>
        {
            var osInfo = await File.ReadAllTextAsync("/etc/os-release");

            var prettyName = osInfo
                .Split('\n')
                .FirstOrDefault(l => l.StartsWith("PRETTY_NAME="))?
                .Split('=')[1]
                .Trim('"') ?? "Unknown";
            
            return prettyName.Trim();
        }, cachedMinutes: _ => TimeSpan.FromDays(1))!;
    }
}