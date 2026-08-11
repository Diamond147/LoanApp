using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;


namespace Infrastructure.HealthChecks
{
    public class PostgresHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public PostgresHealthCheck(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                await command.ExecuteScalarAsync(cancellationToken);

                return HealthCheckResult.Healthy("PostgreSQL connection is healthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("PostgreSQL connection failed.", ex);
            }
        }
    }
}
