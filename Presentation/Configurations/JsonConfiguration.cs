using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Presentation.Configurations;

public static class JsonConfiguration
{
    public static IServiceCollection AddControllersWithJsonOptions(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddEndpointsApiExplorer();

        return services;
    }
}
