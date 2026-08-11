using Application.Exceptions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.Entities;
using Application.Services.Interfaces.ExternalServices;


namespace Application.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly ICacheService _cacheService;

        public AdminService(IAdminRepository adminRepository, ICacheService cacheService)
        {
            _adminRepository = adminRepository;
            _cacheService = cacheService;
        }


        // Dashboard
        public async Task<AdminDashboardStats> GetDashboardStatsAsync()
        {
            string cacheKey = "admin:dashboard:stats";
            var result = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () => await _adminRepository.GetDashboardStatsAsync(),
                expirationTime: TimeSpan.FromMinutes(5)
            );

            if (result == null)
                throw new NotFoundException("Dashboard details not found.");

            return result;
        }

    }
}
