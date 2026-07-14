using Application.Services.Interfaces.Services;
using Domain.Entities;
using Application.Exceptions;
using Domain.DTOs.Users.ResponseDto;
using Application.Services.Interfaces.Repositories;

namespace Application.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepository;

        public DashboardService(IUserRepository userRepository)
        {
            _userRepository=userRepository;
        }
        public async Task<LoanDashboardDto> GetDashboardByIdAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException($"User with ID '{userId}' not found");
            }
            return MapToDashboard(user);
        }
        public async Task<LoanDashboardDto> GetDashboardByEmailAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException($"User with Email '{email}' not found");
            }
            return MapToDashboard(user);
        }
        public async Task<LoanDashboardDto> GetDashboardByMobileAsync(string mobileNumber)
        {
            var user = await _userRepository.GetUserByMobileAsync(mobileNumber);
            if (user == null)
            {
                throw new NotFoundException($"User with Mobile Number '{mobileNumber}' not found");
            }
            return MapToDashboard(user);
        }
        public async Task<LoanDashboardDto> SearchDashboardAsync(string searchTerm)
        {
            var user = await _userRepository.SearchUserAsync(searchTerm);
            if (user == null)
            {
                throw new NotFoundException($"No user found matching '{searchTerm}'");
            }
            return MapToDashboard(user);
        }

        public async Task<IEnumerable<LoanDashboardDto>> GetDashboardsByGenderAsync(string gender)
        {
            var users = await _userRepository.GetUsersByGenderAsync(gender);
            if (!users.Any())
            {
                throw new NotFoundException($"No user found matching '{gender}'");
            }
            return users.Select(MapToDashboard);
        }
        public async Task<IEnumerable<LoanDashboardDto>> GetDashboardsByNationalityAsync(string nationality)
        {
            var users = await _userRepository.GetUsersByNationalityAsync(nationality);
            if (!users.Any())
            {
                throw new NotFoundException($"No user found matching '{nationality}'");
            }
            return users.Select(MapToDashboard);
        }

        //Helper method to map UserProfile to LoanDashboardDto
        private LoanDashboardDto MapToDashboard(UserProfile user)
        {
            return new LoanDashboardDto
            {
                User = new UserProfileDto //Maps UserProfile properties to DTO 
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Gender = user.Gender,
                    Email = user.Email,
                    MobileNumber = user.MobileNumber,
                    DateOfBirth = user.DateOfBirth,
                    Nationality = user.Nationality,
                    SignUpDate = user.SignUpDate
                },

                Loans = user.Loans
                .Select(l => new LoanDto
                {
                    Id = l.Id,
                    LoanType = l.LoanType,
                    RequestedAmount = l.Amount,
                    Status = l.Status,
                    UserProfileId = l.UserProfileId
                }).ToList(),

                LoanHistory = user.Loans
                .SelectMany(l => l.LoanHistories)
                .Select(lh => new LoanHistoryDto
                {
                    Id = lh.Id,
                    LoanType = lh.LoanType,
                    RequestedAmount = lh.RequestedAmount,
                    RequestedDate = lh.RequestedDate,
                    Status = lh.Status, 
                    UserProfileId = lh.UserProfileId
                }).ToList()
            };
        }
    }
}
