using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace Application.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<UserProfileDto> GetOrCreateUserProfileAsync()
        {
            try
            {
                var userInfo = _httpContextAccessor.HttpContext?.User?.GetUserInfo(); // Get all user info at once
                if (userInfo == null)
                    throw new UnauthorizedAccessException("User is not authenticated");

                // Check if profile exists
                var existingProfile = await _userRepository.GetUserByIdAsync(userInfo.UserId);
                if (existingProfile != null)
                {
                    return new UserProfileDto
                    {
                        Id = existingProfile.Id,
                        FirstName = existingProfile.FirstName,
                        LastName = existingProfile.LastName,
                        Email = existingProfile.Email,
                        Gender = existingProfile.Gender,
                        DateOfBirth = existingProfile.DateOfBirth,
                        MobileNumber = existingProfile.MobileNumber,
                        Nationality = existingProfile.Nationality,
                        SignUpDate = existingProfile.SignUpDate
                    };
                }

                // Create new profile
                var userProfile = new UserProfile
                {
                    Id = userInfo.UserId,
                    FirstName = userInfo.FirstName,
                    LastName = userInfo.LastName,
                    Email = userInfo.Email,
                    SignUpDate = DateTime.UtcNow,
                    Gender = null,
                    DateOfBirth = null,
                    MobileNumber = null,
                    Nationality = null
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
                    SignUpDate = userProfile.SignUpDate,
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (CosmosException ex) when (
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        // Complete user profile with additional information
        public async Task<UserProfileDto> CompleteUserProfileAsync(CompleteProfileDto completeProfileDto)
        {
            try
            {
                var userInfo = _httpContextAccessor.HttpContext?.User?.GetUserInfo();
                if (userInfo == null)
                    throw new UnauthorizedAccessException("User is not authenticated");

                // Strip all non-digits just in case
                var digitsOnly = new string(completeProfileDto.MobileNumber.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length > 11)
                {
                    throw new ValidationException("Mobile number cannot exceed 11 digits.");
                }

                var existingUser = await _userRepository.GetUserByIdAsync(userInfo.UserId);
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
            catch (CosmosException ex) when (
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
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
            catch (CosmosException ex) when (
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
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
                var userInfo = _httpContextAccessor.HttpContext?.User?.GetUserInfo();
                if (userInfo == null)
                    throw new UnauthorizedAccessException("User is not authenticated");

                var existingUser = await _userRepository.GetUserByIdAsync(userInfo.UserId);
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
            catch(CosmosException ex) when(
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
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
            catch (CosmosException ex) when (
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
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
            catch (CosmosException ex) when (
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }
    }
}
