using Application.Services.Interfaces.Services;
using Domain.Entities;
using Application.Exceptions;
using Domain.DTOs.Users.ResponseDto;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.ExternalServices;
using AutoMapper;

namespace Application.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;

        public DashboardService(IUserRepository userRepository, ICacheService cacheService, IMapper mapper)
        {
            _userRepository = userRepository;
            _cacheService = cacheService;
            _mapper = mapper;
        }


        public async Task<LoanDashboardDto> GetDashboardByIdAsync(string userId)
        {
            string cacheKey = $"dashboard:id:{userId}";
            var dashboard = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var user = await _userRepository.GetUserByIdAsync(userId);
                    if (user == null)
                        throw new NotFoundException($"User with ID '{userId}' not found");
                    return MapToDashboard(user);
                },
                expirationTime: TimeSpan.FromMinutes(5)
            );

            return dashboard!;
        }

        public async Task<LoanDashboardDto> GetDashboardByEmailAsync(string email)
        {
            string cacheKey = $"dashboard:email:{email}";
            var dashboard = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var user = await _userRepository.GetUserByEmailAsync(email);
                    if (user == null)
                        throw new NotFoundException($"User with Email '{email}' not found");
                    return MapToDashboard(user);
                },
                expirationTime: TimeSpan.FromMinutes(5)
            );

            return dashboard!;
        }

        public async Task<LoanDashboardDto> GetDashboardByMobileAsync(string mobileNumber)
        {
            string cacheKey = $"dashboard:mobile:{mobileNumber}";
            var dashboard = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var user = await _userRepository.GetUserByMobileAsync(mobileNumber);
                    if (user == null)
                        throw new NotFoundException($"User with Mobile Number '{mobileNumber}' not found");
                    return MapToDashboard(user);
                },
                expirationTime: TimeSpan.FromMinutes(5)
            );

            return dashboard!;
        }

        public async Task<LoanDashboardDto> SearchDashboardAsync(string searchTerm)
        {
            string cacheKey = $"dashboard:search:{searchTerm}";
            var dashboard = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var user = await _userRepository.SearchUserAsync(searchTerm);
                    if (user == null)
                        throw new NotFoundException($"No user found matching '{searchTerm}'");
                    return MapToDashboard(user);
                },
                expirationTime: TimeSpan.FromMinutes(5)
            );

            return dashboard!;
        }

        public async Task<IEnumerable<LoanDashboardDto>> GetDashboardsByGenderAsync(string gender)
        {
            string cacheKey = $"dashboard:gender:{gender}";
            var list = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var users = await _userRepository.GetUsersByGenderAsync(gender);
                    if (!users.Any())
                        throw new NotFoundException($"No user found matching '{gender}'");
                    return users.Select(MapToDashboard).ToList();
                },
                expirationTime: TimeSpan.FromMinutes(5)
            );

            return list ?? Enumerable.Empty<LoanDashboardDto>();
        }

        public async Task<IEnumerable<LoanDashboardDto>> GetDashboardsByNationalityAsync(string nationality)
        {
            string cacheKey = $"dashboard:nationality:{nationality}";
            var list = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var users = await _userRepository.GetUsersByNationalityAsync(nationality);
                    if (!users.Any())
                        throw new NotFoundException($"No user found matching '{nationality}'");
                    return users.Select(MapToDashboard).ToList();
                },
                expirationTime: TimeSpan.FromMinutes(5)
            );

            return list ?? Enumerable.Empty<LoanDashboardDto>();
        }


        //Helper method to map UserProfile to LoanDashboardDto
        private LoanDashboardDto MapToDashboard(UserProfile user)
        {
            return new LoanDashboardDto
            {
                User = _mapper.Map<UserProfileDto>(user),

                Loans = user.Loans.Select(l => _mapper.Map<LoanDto>(l)).ToList(),

                LoanHistory = user.Loans
                .SelectMany(l => l.LoanHistories)
                .Select(lh => _mapper.Map<LoanHistoryDto>(lh)).ToList()
            };
        }
    }
}
