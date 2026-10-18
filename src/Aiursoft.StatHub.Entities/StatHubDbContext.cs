using System.Diagnostics.CodeAnalysis;
using Aiursoft.StatHub.Entities.Monitoring;
using Aiursoft.DbTools;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.StatHub.Entities;

[ExcludeFromCodeCoverage]

public abstract class StatHubDbContext(DbContextOptions options) : IdentityDbContext<User>(options), ICanMigrate
{
    public DbSet<MonitoringPolicy> AlertMonitoringPolicies => Set<MonitoringPolicy>();
    public DbSet<MonitoringRule> AlertMonitoringRules => Set<MonitoringRule>();
    public DbSet<MonitoringTarget> AlertMonitoringTargets => Set<MonitoringTarget>();
    public DbSet<AgentMonitoringState> AgentMonitoringStates => Set<AgentMonitoringState>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<MonitoringTarget>().HasKey(item => new { item.PolicyId, item.ClientId });
    }

    public DbSet<GlobalSetting> GlobalSettings => Set<GlobalSetting>();

    public virtual  Task MigrateAsync(CancellationToken cancellationToken) =>
        Database.MigrateAsync(cancellationToken);

    public virtual  Task<bool> CanConnectAsync() =>
        Database.CanConnectAsync();
}
