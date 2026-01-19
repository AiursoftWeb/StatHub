using Aiursoft.StatHub.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.StatHub.InMemory;

public class InMemoryContext(DbContextOptions<InMemoryContext> options) : StatHubDbContext(options)
{
    public override Task MigrateAsync(CancellationToken cancellationToken)
    {
        return Database.EnsureCreatedAsync(cancellationToken);
    }

    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
