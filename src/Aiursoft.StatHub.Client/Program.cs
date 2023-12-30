using System.Diagnostics.CodeAnalysis;
using Aiursoft.CommandFramework;

[ExcludeFromCodeCoverage]
public class Program
{
    public static Task<int> Main(string[] args)
    {
        return new SingleCommandApp<ClientHandler>()
            .WithDefaultOption(ClientHandler.Server)
            .RunAsync(args);
    }
}