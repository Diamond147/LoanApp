using Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Presentation.Configurations;


public static class HealthChecksConfiguration
{
    public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddHttpClient<PaystackHealthCheck>();

        services.AddHealthChecks()
            .AddCheck("postgres_db", new PostgresHealthCheck(connectionString!), tags: new[] { "ready" })
            .AddCheck<PaystackHealthCheck>("paystack_api", tags: new[] { "ready" });

        return services;
    }
}
