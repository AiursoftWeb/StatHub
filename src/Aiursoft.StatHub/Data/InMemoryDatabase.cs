using System.Collections.Concurrent;
using Aiursoft.Scanner.Abstractions;
using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Data;

public class InMemoryDatabase : ISingletonDependency
{
    private ConcurrentDictionary<string, Agent> Clients { get; } = new();

    public Agent GetOrAddClient(string clientId)
    {
        lock (Clients)
        {
            return Clients.GetOrAdd(clientId, _ => new Agent(clientId));
        }
    }

    public Agent[] GetClients()
    {
        return Clients.Select(c => c.Value).ToArray();
    }
}
