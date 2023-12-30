using System.Collections.Concurrent;
using Aiursoft.Scanner.Abstractions;
using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Server.Data;

public class InMemoryDatabase : ISingletonDependency
{
    private ConcurrentDictionary<string, Client> Clients { get; } = new();

    public Client GetOrAddClient(string ipWithHost)
    {
        lock (Clients)
        {
            return Clients.GetOrAdd(ipWithHost, _ => new Client());
        }
    }
    
    public Client[] GetClients()
    {
        return Clients.Select(c => c.Value).ToArray();
    }
}