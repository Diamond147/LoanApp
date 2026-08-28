using Application.Mappings;
using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Services;
using Infrastructure.ExternalServices.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;

namespace Presentation.Configurations;

public static class ServicesConfiguration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register application services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPaystackWebhook, PaystackWebhook>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPrequalifiedLoanService, PrequalifiedLoanService>();
        services.AddScoped<ILoanHistoryService, LoanHistoryService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        // AutoMapper configuration
        services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>(), typeof(AutoMapperProfile).Assembly);

        return services;
    }
}
