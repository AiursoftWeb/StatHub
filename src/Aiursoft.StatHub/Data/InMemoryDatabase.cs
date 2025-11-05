using System.Collections.Concurrent;
using Aiursoft.Scanner.Abstractions;
using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Data;

public class InMemoryDatabase : ISingletonDependency
{
    private ConcurrentDictionary<string, Agent> Agents { get; } = new();

    public Agent GetOrAddClient(string clientId)
    {
        lock (Agents)
        {
            return Agents.GetOrAdd(clientId, _ => new Agent(clientId));
        }
    }

    public Agent? GetClient(string clientId)
    {
        Agents.TryGetValue(clientId, out var client);
        return client;
    }

    public Agent[] GetAgents()
    {
        return Agents.Select(c => c.Value).ToArray();
    }
}
