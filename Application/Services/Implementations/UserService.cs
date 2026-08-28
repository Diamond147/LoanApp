using Application.DTOs;
using Application.Exceptions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Application.Services.Interfaces.ExternalServices;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using AutoMapper;
using System.Globalization;


namespace Application.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, ICacheService cacheService, IMapper mapper)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _cacheService = cacheService;
            _mapper = mapper;
        }


        // All Users Details with their Loans and Loan Histories
        public async Task<ContinuationResponse<AllUserDetailsDto>> GetAllUsersDetailsAsync(int pageSize, string? continuationToken, string? userId, string? email, string? mobileNumber, string? gender, string? nationality, string? searchTerm)
        {
            if (pageSize < 1 || pageSize > 100)
            {
                throw new ValidationException("PageSize must be between 1 and 100.");
            }

            // Build cache key from ALL query parameters, same pattern as GetAllUsersAsync
            string cacheKey = $"users:details:page={pageSize}" +
                               $":token={continuationToken ?? "none"}" +
                               $":id={userId ?? "none"}" +
                               $":email={email ?? "none"}" +
                               $":mobile={mobileNumber ?? "none"}" +
                               $":gender={gender ?? "none"}" +
                               $":nationality={nationality ?? "none"}" +
                               $":search={searchTerm ?? "none"}";

            var result = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    // Get users with their loans
                    var (users, nextToken) = await _userRepository.GetAllUsersDetailsAsync(pageSize, continuationToken, userId, email, mobileNumber, gender, nationality, searchTerm);
                    if (!users.Any())
                    {
                        return new ContinuationResponse<AllUserDetailsDto>
                        {
                            Data = new List<AllUserDetailsDto>(),
                            ContinuationToken = null,
                            HasMore = false
                        };
                    }

                    var userDetails = new List<AllUserDetailsDto>();

                    foreach (var user in users)
                    {
                        // Get all histories from all loans for this user
                        var histories = user.Loans?
                            .SelectMany(l => l.LoanHistories ?? new List<LoanHistory>())
                            .ToList() ?? new List<LoanHistory>();

                        var userDetail = _mapper.Map<AllUserDetailsDto>(user);
                        {
                            // Map loans
                            userDetail.Loans = user.Loans?.Select(l => _mapper.Map<LoanDto>(l))
                            .OrderByDescending(l => l.RequestedDate)
                            .ToList() ?? new List<LoanDto>();
                            
                            // Map loan histories
                            userDetail.LoanHistories = (histories ?? Enumerable.Empty<LoanHistory>())
                            .Select(h => _mapper.Map<LoanHistoryDto>(h))
                            .OrderByDescending(h => h.RequestedDate)
                            .ToList() ?? new List<LoanHistoryDto>();
                        };
                        userDetails.Add(userDetail);
                    }

                    return new ContinuationResponse<AllUserDetailsDto>
                    {
                        Data = userDetails,
                        ContinuationToken = nextToken,
                        HasMore = nextToken != null,
                    };
                },
                expirationTime: TimeSpan.FromMinutes(10)
            );

            return result ?? new ContinuationResponse<AllUserDetailsDto> { Data = new List<AllUserDetailsDto>(), ContinuationToken = null, HasMore = false };
        }



        // User Management
        public async Task<ContinuationResponse<UserProfileDto>> GetAllUsersAsync(int pageSize, string? continuationToken, string? userId)
        {
            if (pageSize < 1 || pageSize > 100)
            {
                throw new ValidationException("Page size must be between 1 and 100.");
            }
            string cacheKey = $"users:all:page={pageSize}:token={continuationToken ?? "none"}:userid={userId ?? "none"}";

            var result = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var (users, newContinuationToken) = await _userRepository.GetAllUsersAsync(pageSize, continuationToken, userId);

                    var userDtos = new List<UserProfileDto>();

                    foreach (var user in users)
                    {
                        var Dto = _mapper.Map<UserProfileDto>(user);
                        userDtos.Add(Dto);
                    }
                    return new ContinuationResponse<UserProfileDto>
                    {
                        Data = userDtos,
                        ContinuationToken = newContinuationToken,
                        HasMore = !string.IsNullOrEmpty(newContinuationToken)
                    };
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

            return result ?? new ContinuationResponse<UserProfileDto> { Data = new List<UserProfileDto>(), ContinuationToken = null, HasMore = false };
        }


        // Changing user role from "Admin" to "User"
        public async Task ChangeUserRoleAsync(string UserId, ChangeRoleDto dto)
        {
            // Input Validation
            if (string.IsNullOrEmpty(dto.NewRole))
            {
                throw new ArgumentException("Role cannot be empty.");
            }

            // Normalize role input to ensure case-insensitivity
            var normalizedRole = char.ToUpper(dto.NewRole[0]) + dto.NewRole.Substring(1).ToLower();
            if (normalizedRole != "Admin" && normalizedRole != "User")
            {
                throw new ArgumentException("Invalid role. Allowed roles are 'Admin' or 'User'.");
            }

            // Fetch User
            var user = await _userRepository.GetUserByIdAsync(UserId);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {UserId} not found.");
            }

            user.Role = normalizedRole;

            await _userRepository.UpdateUserAsync(user);

            // Invalidate caches for this user and user lists
            await _cacheService.RemoveAsync($"users:id:{UserId}");
            await _cacheService.RemoveByPrefixAsync("users:all:");
            await _cacheService.RemoveByPrefixAsync("users:details:");
        }


        public async Task<UserProfileDto?> GetUserByIdAsync(string userId)
        {
            string cacheKey = $"users:id:{userId}";

            var userDto = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var user = await _userRepository.GetUserByIdAsync(userId);
                    if (user == null)
                        throw new NotFoundException("User not found");

                    return _mapper.Map<UserProfileDto>(user);
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

            return userDto;
        }


        // Update user profile (for existing users to change their info)
        public async Task<UserProfileDto> UpdateUserAsync(UpdateUserProfileDto updateUser)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not authenticated");

            var existingUser = await _userRepository.GetUserByIdAsync(userId);
            if (existingUser == null)
            {
                throw new NotFoundException("User profile not found");
            }

            // Update only provided fields
            //if (!string.IsNullOrEmpty(updateUser.Gender))
            //    existingUser.Gender = updateUser.Gender;

            //if (updateUser.DateOfBirth != null)
            //    existingUser.DateOfBirth = updateUser.DateOfBirth;

            if (!string.IsNullOrEmpty(updateUser.MobileNumber))
                existingUser.MobileNumber = updateUser.MobileNumber;

            if (!string.IsNullOrEmpty(updateUser.Nationality))
                existingUser.Nationality = updateUser.Nationality;

            await _userRepository.UpdateUserAsync(existingUser);

            // Invalidate caches
            await _cacheService.RemoveAsync($"users:id:{existingUser.Id}");
            await _cacheService.RemoveByPrefixAsync("users:all:");
            await _cacheService.RemoveByPrefixAsync("users:details:");

            return _mapper.Map<UserProfileDto>(existingUser);
        }


        public async Task<UserProfileDto> PatchUserAsync(string userId, PatchUserProfileDto patchUser)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            if (patchUser.FirstName != null) user.FirstName = patchUser.FirstName;
            if (patchUser.LastName != null) user.LastName = patchUser.LastName;
            if (patchUser.MobileNumber != null) user.MobileNumber = patchUser.MobileNumber;
            //if (patchUser.Gender != null) user.Gender = patchUser.Gender;
            //if (patchUser.Nationality != null) user.Nationality = patchUser.Nationality;
            //if (patchUser.DateOfBirth != null) user.DateOfBirth = patchUser.DateOfBirth.Value;

            await _userRepository.PatchUserAsync(user);

            // Invalidate caches
            await _cacheService.RemoveAsync($"users:id:{userId}");
            await _cacheService.RemoveByPrefixAsync("users:all:");
            await _cacheService.RemoveByPrefixAsync("users:details:");

            return _mapper.Map<UserProfileDto>(user);
        }


        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userRepository.DeleteUserAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            // Invalidate caches
            await _cacheService.RemoveAsync($"users:id:{userId}");
            await _cacheService.RemoveByPrefixAsync("users:all:");
            await _cacheService.RemoveByPrefixAsync("users:details:");

            return true;
        }
    }
}
