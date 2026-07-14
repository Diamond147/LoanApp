using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.IO;

namespace Infrastructure.DbContexts
{
    public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Load .env (if present) so environment variables are available to configuration providers
            LoadEnvFileIfPresent();


            // This avoids hardcoding values in the factory and lets developers keep secrets in .env or user-secrets.
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Prefer a single explicit connection string if present
            var connectionString = config["DB_CONNECTION_STRING"]
                                   ?? config.GetConnectionString("DefaultConnection")
                                   ?? config["ConnectionStrings:DefaultConnection"]
                                   ?? config["ConnectionStrings:Postgres"];

            // If no single connection string, try to compose from individual configuration values
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var dbHost = config["DB_HOST"];
                var dbPort = config["DB_PORT"];
                var dbName = config["DB_NAME"];
                var dbUser = config["DB_USER"];
                var dbPassword = config["DB_PASSWORD"];

                if (!string.IsNullOrWhiteSpace(dbHost)
                    && !string.IsNullOrWhiteSpace(dbPort)
                    && !string.IsNullOrWhiteSpace(dbName)
                    && !string.IsNullOrWhiteSpace(dbUser)
                    && !string.IsNullOrWhiteSpace(dbPassword))
                {
                    var builder = new NpgsqlConnectionStringBuilder
                    {
                        Host = dbHost,
                        Port = int.TryParse(dbPort, out var p) ? p : 5432,
                        Database = dbName,
                        Username = dbUser,
                        Password = dbPassword
                    };

                    connectionString = builder.ConnectionString;
                }
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("No database connection configuration found. Set DB_CONNECTION_STRING or the DB_* variables in environment or appsettings.json.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }

        private void LoadEnvFileIfPresent()
        {
            try
            {
                // Try a set of likely base directories where a .env may live (current, base dir, assembly location)
                var candidates = new List<string>
                {
                    Directory.GetCurrentDirectory(),
                    AppContext.BaseDirectory,
                    Path.GetDirectoryName(typeof(DesignTimeAppDbContextFactory).Assembly.Location) ?? string.Empty
                };

                var rootSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var start in candidates.Where(s => !string.IsNullOrEmpty(s)))
                {
                    var dir = start;
                    var root = Directory.GetDirectoryRoot(dir);
                    while (dir != null && !rootSeen.Contains(dir))
                    {
                        rootSeen.Add(dir);
                        var envPath = Path.Combine(dir, ".env");
                        if (File.Exists(envPath))
                        {
                            // For debugging/design-time visibility, write which .env was used (does not print secrets)
                            //try { Console.WriteLine($"[DesignTime] Loaded .env from: {envPath}"); } catch { }

                            foreach (var line in File.ReadAllLines(envPath))
                            {
                                var trimmed = line.Trim();
                                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                                    continue;

                                var idx = trimmed.IndexOf('=');
                                if (idx <= 0)
                                    continue;

                                var key = trimmed.Substring(0, idx).Trim();
                                var value = trimmed.Substring(idx + 1).Trim().Trim('"');

                                // Override process environment variables with .env values so design-time tools use them
                                Environment.SetEnvironmentVariable(key, value);
                            }

                            return; // stop after first .env found
                        }

                        dir = Directory.GetParent(dir)?.FullName;
                    }
                }
            }
            catch
            {
                // Swallow any errors here — design-time context should not crash because of .env parsing
            }
        }
    }
}
