using Finance.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Finance.Api.HealthChecks
{
    public class DbHealthCheck : IHealthCheck
    {
        private readonly FinanceDbContext _dbContext;

        public DbHealthCheck(FinanceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to open a connection to the database
                var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
                if (canConnect)
                {
                    return HealthCheckResult.Healthy("Database is reachable");
                }

                return HealthCheckResult.Unhealthy("Cannot connect to the database");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Database check failed: {ex.Message}");
            }
        }
    }
}
