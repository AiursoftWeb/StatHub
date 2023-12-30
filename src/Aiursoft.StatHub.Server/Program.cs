using System.Diagnostics.CodeAnalysis;
using Aiursoft.WebTools;

namespace Aiursoft.StatHub.Server;

[ExcludeFromCodeCoverage]
public class Program
{
    public static async Task Main(string[] args)
    {
        var app = Extends.App<Startup>(args);
        await app.RunAsync();
    }
}