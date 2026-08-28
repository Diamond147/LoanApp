using Application.Services.Interfaces.ExternalServices;
using Infrastructure.ExternalServices;
using Infrastructure.ExternalServices.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Presentation.Configurations;

public static class ExternalServicesConfiguration
{
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Paystack HttpClient registration
        services.AddHttpClient<IPaystackClient, PaystackClient>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        // Register PaystackClient factory to inject the secret key from env/config
        services.AddScoped<IPaystackClient>(provider =>
        {
            var httpClient = provider.GetRequiredService<HttpClient>();
            var secretKey = Environment.GetEnvironmentVariable("Paystack__SecretKey")
                ?? configuration["Paystack:SecretKey"]
                ?? throw new InvalidOperationException("Paystack__SecretKey environment variable is missing");

            return new PaystackClient(httpClient, secretKey);
        });


        // SendGrid / Email client registration (fallback to NoOp)
        services.AddScoped<IEmailClient>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var apiKey = Environment.GetEnvironmentVariable("SendGrid__ApiKey") ?? config["SendGrid:ApiKey"];
            var senderEmail = Environment.GetEnvironmentVariable("SendGrid__SenderEmail") ?? config["SendGrid:SenderEmail"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(senderEmail))
            {
                return new NoOpEmailClient();
            }

            return new SendGridEmailClient(apiKey, senderEmail);
        });

        return services;
    }
}
