using Application.Exceptions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.Entities;


namespace Application.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;

        public AdminService(IAdminRepository adminRepository)
        {
            _adminRepository=adminRepository;
        }



        // Dashboard
        public async Task<AdminDashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                var result = await _adminRepository.GetDashboardStatsAsync();
                if (result == null)
                    throw new NotFoundException("Dashboard details not found.");
                return result;
            }
            catch (Exception ex) when (ex is NotFoundException) 
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

    }
}
