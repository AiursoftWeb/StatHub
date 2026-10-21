using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.Entities.MetricsExport;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.StatHub.Services.MetricsExport;

public sealed class MetricsExportPolicyService(StatHubDbContext db, MetricsExportBuffer buffer,
    MetricsExportPolicyCoordinator coordinator)
{
    public async Task<MetricsExportPolicy> PolicyAsync()
    {
        var policy = await db.MetricsExportPolicies.Include(item => item.Targets)
            .SingleOrDefaultAsync(item => item.Id == MetricsExportPolicy.DefaultId);
        if (policy != null) return policy;
        policy = new MetricsExportPolicy();
        db.MetricsExportPolicies.Add(policy);
        await db.SaveChangesAsync();
        return policy;
    }

    public async Task RefreshAsync()
    {
        await coordinator.Gate.WaitAsync();
        try { await buffer.ApplyPolicyAsync(await PolicyAsync()); }
        finally { coordinator.Gate.Release(); }
    }
}
