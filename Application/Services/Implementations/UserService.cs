using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Azure.Core;
using BCrypt.Net;
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

        private string HashPassword(string password)
        {
            // Generates a cryptographically secure, unique salt automatically and hashes it
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string Password, string PasswordHash)
        {
            // Extracts the salt from the stored hash and validates the comparison mathematically
            if (string.IsNullOrEmpty(PasswordHash)) return false;

            return BCrypt.Net.BCrypt.Verify(Password, PasswordHash);
        }


        public async Task<UserProfileDto> CreateUserProfileAsync(CreateUserProfileDto createUserProfileDto)
        {
            try
            {
                // Validate if user email already exists
                var existingUser = await _userRepository.GetUserByEmailAsync(createUserProfileDto.Email);
                if (existingUser != null)
                {
                    throw new InvalidOperationException("A user with this email already exists.");
                }

                // Create new profile
                var userProfile = new UserProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = createUserProfileDto.FirstName,
                    LastName = createUserProfileDto.LastName,
                    Email = createUserProfileDto.Email,
                    PasswordHash = HashPassword(createUserProfileDto.Password),
                    Gender = createUserProfileDto.Gender,
                    DateOfBirth = createUserProfileDto.DateOfBirth,
                    MobileNumber = createUserProfileDto.MobileNumber,
                    Nationality = createUserProfileDto.Nationality,
                    SignUpDate = DateTime.UtcNow
                };

                await _userRepository.AddUserAsync(userProfile);
                return new UserProfileDto
                {
                    Id = userProfile.Id,
                    FirstName = userProfile.FirstName,
                    LastName = userProfile.LastName,
                    Email = userProfile.Email,
                    Gender = userProfile.Gender,
                    DateOfBirth = userProfile.DateOfBirth,
                    MobileNumber = userProfile.MobileNumber,
                    Nationality = userProfile.Nationality,
                    SignUpDate = DateTime.UtcNow
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException("Service is temporarily unavailable. Please try again later.", ex);
            }
        }


        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials");

            // Use the token service to mint the JWT string
            var roles = new[] { "User" };
            var token = _tokenService.GenerateAccessToken(user.Id, user.Email, roles);

            return new LoginResponseDto {
                AccessToken = token,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }


        // Complete user profile with additional information
        public async Task<UserProfileDto> CompleteUserProfileAsync(CompleteProfileDto completeProfileDto)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("User is not authenticated");

                // Strip all non-digits just in case
                var digitsOnly = new string(completeProfileDto.MobileNumber.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length > 11)
                {
                    throw new ValidationException("Mobile number cannot exceed 11 digits.");
                }

                var existingUser = await _userRepository.GetUserByIdAsync(userId);
                if (existingUser == null)
                {
                    throw new NotFoundException("User profile not found. Please sign in first.");
                }

                // Update optional fields
                existingUser.Gender = completeProfileDto.Gender;
                existingUser.DateOfBirth = completeProfileDto.DateOfBirth;
                existingUser.MobileNumber = completeProfileDto.MobileNumber;
                existingUser.Nationality = completeProfileDto.Nationality;

                await _userRepository.UpdateUserProfileAsync(existingUser);

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
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException("Service is temporarily unavailable. Please try again later.", ex);
            }
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
                if (!string.IsNullOrEmpty(updateUser.Gender))
                    existingUser.Gender = updateUser.Gender;

                if (updateUser.DateOfBirth != null)
                    existingUser.DateOfBirth = updateUser.DateOfBirth;

                if (!string.IsNullOrEmpty(updateUser.MobileNumber))
                    existingUser.MobileNumber = updateUser.MobileNumber;

                if (!string.IsNullOrEmpty(updateUser.Nationality))
                    existingUser.Nationality = updateUser.Nationality;

                await _userRepository.UpdateUserProfileAsync(existingUser);

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
                if (patchUser.Gender != null) user.Gender = patchUser.Gender;
                if (patchUser.MobileNumber != null) user.MobileNumber = patchUser.MobileNumber;
                if (patchUser.Nationality != null) user.Nationality = patchUser.Nationality;
                if (patchUser.DateOfBirth != null) user.DateOfBirth = patchUser.DateOfBirth.Value;

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
