using Aiursoft.DbTools;
using Aiursoft.DbTools.InMemory;
using Aiursoft.StatHub.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.StatHub.InMemory;

public class InMemorySupportedDb : SupportedDatabaseType<StatHubDbContext>
{
    public override string DbType => "InMemory";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurInMemoryDb<InMemoryContext>();
    }

    public override StatHubDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<InMemoryContext>();
    }
}
