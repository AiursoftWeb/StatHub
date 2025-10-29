using Aiursoft.StatHub.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.StatHub.Sqlite;

public class SqliteContext(DbContextOptions<SqliteContext> options) : TemplateDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
