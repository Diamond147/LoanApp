using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;


namespace Presentation.Configurations;


public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Fetch the PostgreSQL connection string from environment variables or appsettings.json
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Infrastructure"));

            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.LogTo(Console.WriteLine, LogLevel.Information);
            }
        });

        return services;
    }
}
