using Application.Exceptions;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Microsoft.AspNetCore.Http;


namespace Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _tokenService;
        public AuthService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, ITokenService tokenService)
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

                var isFirstUser = !await _userRepository.AnyAsync();

                // Create new profile
                var userProfile = new UserProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = createUserProfileDto.FirstName,
                    LastName = createUserProfileDto.LastName,
                    Email = createUserProfileDto.Email,
                    PasswordHash = HashPassword(createUserProfileDto.Password),
                    Role = isFirstUser ? "Admin" : "User", // Automatic promotion for the first account
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
            var roles = new[] { user.Role ?? "User" };
            var token = _tokenService.GenerateAccessToken(user.Id, user.Email, roles);

            var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is unavailable.");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            };

            // Inject the token safely into the encrypted cookie jar
            httpContext.Response.Cookies.Append("X-Access-Token", token, cookieOptions);

            // Return profile data to the frontend without exposing the raw token string
            return new LoginResponseDto
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }


        public Task LogoutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("HTTP context is unavailable.");

            // Expire the cookie instantly to force browser deletion
            httpContext.Response.Cookies.Append("X-Access-Token", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            });

            return Task.CompletedTask;
        }
    }
}
