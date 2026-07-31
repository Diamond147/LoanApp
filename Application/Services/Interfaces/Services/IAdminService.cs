
using Domain.Entities;


namespace Application.Services.Interfaces.Services
{
    public interface IAdminService
    {
        // Dashboard
        Task<AdminDashboardStats> GetDashboardStatsAsync();
    }
}
