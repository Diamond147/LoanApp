using Domain.DTOs.Users.ResponseDto;

namespace Application.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<LoanDashboardDto> GetDashboardByIdAsync(string userId);
        Task<LoanDashboardDto> GetDashboardByEmailAsync(string email);
        Task<LoanDashboardDto> GetDashboardByMobileAsync(string mobileNumber);
        Task<LoanDashboardDto> SearchDashboardAsync(string searchTerm);
        Task<IEnumerable<LoanDashboardDto>> GetDashboardsByGenderAsync(string gender);
        Task<IEnumerable<LoanDashboardDto>> GetDashboardsByNationalityAsync(string nationality);
    }
}
