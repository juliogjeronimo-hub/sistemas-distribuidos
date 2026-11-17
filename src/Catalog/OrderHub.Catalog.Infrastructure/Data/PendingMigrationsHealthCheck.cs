using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OrderHub.Catalog.Infrastructure.Data;

// The service must not report ready while the schema is behind the model: requests would
// fail with "relation does not exist" instead of being held back by the load balancer.
internal sealed class PendingMigrationsHealthCheck(CatalogDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        return pending.Count == 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"Pending migrations: {string.Join(", ", pending)}");
    }
}
