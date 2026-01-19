using System.Diagnostics.CodeAnalysis;
using Aiursoft.DbTools;
using Aiursoft.StatHub.Entities;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.StatHub;

[ExcludeFromCodeCoverage]
public abstract class Program
{
    public static async Task Main(string[] args)
    {
        var app = await AppAsync<Startup>(args);
        await app.UpdateDbAsync<StatHubDbContext>();
        await app.SeedAsync();
        await app.CopyAvatarFileAsync();
        await app.RunAsync();
    }
}
