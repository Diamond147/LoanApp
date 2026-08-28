using Application.Services.Interfaces.Repositories;
using Infrastructure.Repositories.Implementations;
using Infrastructure.Repositories.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Configurations;

public static class RepositoriesConfiguration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IPrequalifiedLoanRepo, PrequalifiedLoanRepo>();
        services.AddScoped<ILoanHistoryRepository, LoanHistoryRepository>();

        return services;
    }
}
