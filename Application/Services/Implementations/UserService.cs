using Application.DTOs;
using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Azure.Core;
using BCrypt.Net;
using Domain.DTOs.Admin;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _tokenService;
        public UserService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
        }


        // All Users Details with their Loans and Loan Histories
        public async Task<ContinuationResponse<UserProfileDto>> GetAllUsersDetailsAsync(
            int pageSize, string? continuationToken, string? userId, string? email, string? mobileNumber, string? gender, string? nationality, string? searchTerm)
        {
            try
            {
                if (pageSize < 1 || pageSize > 100)
                {
                    throw new ValidationException("PageSize must be between 1 and 100.");
                }

                // Get users with their loans
                var (users, nextToken) = await _userRepository.GetAllUsersDetailsAsync(pageSize, continuationToken, userId, email, mobileNumber, gender, nationality, searchTerm);
                if (!users.Any())
                {
                    return new ContinuationResponse<UserProfileDto>
                    {
                        Data = new List<UserProfileDto>(),
                        ContinuationToken = null,
                        HasMore = false
                    };
                }

                var userDetails = new List<UserProfileDto>();

                foreach (var user in users)
                {
                    // Get all histories from all loans for this user
                    var histories = user.Loans?
                        .SelectMany(l => l.LoanHistories ?? new List<LoanHistory>())
                        .ToList() ?? new List<LoanHistory>();

                    var userDetail = new UserProfileDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        MobileNumber = user.MobileNumber,
                        Gender = user.Gender,
                        DateOfBirth = user.DateOfBirth,
                        Nationality = user.Nationality,
                        SignUpDate = user.SignUpDate,

                        // Map loans
                        Loans = user.Loans?.Select(l => new LoanDto
                        {
                            Id = l.Id,
                            LoanType = l.LoanType,
                            RequestedAmount = l.RequestedAmount,
                            //ApprovedAmount = l.ApprovedAmount,
                            Status = l.Status,
                            RequestedDate = l.RequestedDate,
                            UpdatedDate = l.UpdatedDate,
                            UserProfileId = l.UserProfileId,
                            //UserName = $"{user.FirstName} {user.LastName}"
                        })
                            .OrderByDescending(l => l.RequestedDate)
                            .ToList() ?? new List<LoanDto>(),

                        // Map loan histories
                        LoanHistories = (histories ?? Enumerable.Empty<LoanHistory>())
                        .Select(h => new LoanHistoryDto
                        {
                            Id = h.Id,
                            LoanId = h.LoanId,
                            LoanType = h.LoanType,
                            RequestedAmount = h.RequestedAmount,
                            //ApprovedAmount = h.ApprovedAmount,
                            RequestedDate = h.RequestedDate,
                            UpdatedDate = h.UpdatedDate,
                            Status = h.Status,
                            UserProfileId = h.UserProfileId,
                        }).OrderByDescending(h => h.RequestedDate)
                        .ToList() ?? new List<LoanHistoryDto>(),
                    };
                    userDetails.Add(userDetail);
                }
                return new ContinuationResponse<UserProfileDto>
                {
                    Data = userDetails,
                    ContinuationToken = nextToken,
                    HasMore = nextToken != null,
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        // User Management
        public async Task<ContinuationResponse<UserProfileDto>> GetAllUsersAsync(int pageSize, string? continuationToken, string? userId)
        {
            try
            {
                if (pageSize < 1 || pageSize > 100)
                {
                    throw new ValidationException("Page size must be between 1 and 100.");
                }
                var (users, newContinuationToken) = await _userRepository.GetAllUsersAsync(pageSize, continuationToken, userId);

                var userDtos = new List<UserProfileDto>();

                foreach (var user in users)
                {
                    var newUser = await _userRepository.GetUserByIdAsync(user.Id);

                    userDtos.Add(new UserProfileDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Gender = user.Gender,
                        DateOfBirth = user.DateOfBirth,
                        MobileNumber = user.MobileNumber,
                        SignUpDate = user.SignUpDate,
                        Nationality = user.Nationality,
                    });
                }
                return new ContinuationResponse<UserProfileDto>
                {
                    Data = userDtos,
                    ContinuationToken = newContinuationToken,
                    HasMore = !string.IsNullOrEmpty(newContinuationToken)
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
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
        }


        public async Task<UserProfileDto?> GetUserByIdAsync(string userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new NotFoundException("User not found");
                }
                return new UserProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Gender = user.Gender,
                    Email = user.Email,
                    MobileNumber = user.MobileNumber,
                    SignUpDate = user.SignUpDate,
                    Nationality = user.Nationality,
                    DateOfBirth = user.DateOfBirth
                };
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        // Update user profile (for existing users to change their info)
        public async Task<UserProfileDto> UpdateUserAsync(UpdateUserProfileDto updateUser)
        {
            try
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

                return new UserProfileDto
                {
                    Id = existingUser.Id,
                    FirstName = existingUser.FirstName,
                    LastName = existingUser.LastName,
                    Email = existingUser.Email,
                    Gender = existingUser.Gender,
                    DateOfBirth = existingUser.DateOfBirth,
                    MobileNumber = existingUser.MobileNumber,
                    Nationality = existingUser.Nationality,
                    SignUpDate = existingUser.SignUpDate,
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<UserProfileDto> PatchUserAsync(string userId, PatchUserProfileDto patchUser)
        {
            try
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
                return new UserProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Gender = user.Gender,
                    Email = user.Email,
                    MobileNumber = user.MobileNumber,
                    Nationality = user.Nationality,
                    SignUpDate = user.SignUpDate,
                    DateOfBirth = user.DateOfBirth
                };
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userRepository.DeleteUserAsync(userId);
                if (user == null)
                    throw new NotFoundException("User not found");

                return true;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }
    }
}
