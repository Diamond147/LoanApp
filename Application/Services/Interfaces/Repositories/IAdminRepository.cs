using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Interfaces.Repositories
{
    public interface IAdminRepository
    {

        // Dashboard Statistics
        Task<AdminDashboardStats> GetDashboardStatsAsync();

    }
}
