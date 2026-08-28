using Application.Exceptions;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using AutoMapper;
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
        private readonly IMapper _mapper;

        public AuthService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, ITokenService tokenService, IMapper mapper)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            _mapper = mapper;
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
            // Validate if user email already exists
            var existingUser = await _userRepository.GetUserByEmailAsync(createUserProfileDto.Email);
            if (existingUser != null)
            {
                throw new ConflictException("A user with this email already exists.");
            }

            var isFirstUser = !await _userRepository.AnyAsync();

            // Use AutoMapper to map incoming DTO to entity, then apply generated fields
            var userProfile = _mapper.Map<UserProfile>(createUserProfileDto);

            userProfile.Id = Guid.NewGuid().ToString();
            userProfile.PasswordHash = HashPassword(createUserProfileDto.Password);
            userProfile.Role = isFirstUser ? "Admin" : "User"; // Automatic promotion for the first account
            userProfile.SignUpDate = DateTime.UtcNow;

            await _userRepository.AddUserAsync(userProfile);

            // Map saved entity back to response DTO
            return _mapper.Map<UserProfileDto>(userProfile);
        }


        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                throw new ValidationException("Invalid credentials");

            // Use the token service to mint the JWT string
            var roles = new[] { user.Role ?? "User" };
            var token = _tokenService.GenerateAccessToken(user.Id, user.Email, roles);

            var httpContext = _httpContextAccessor.HttpContext
            ?? throw new ValidationException("HTTP context is unavailable.");

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
            return _mapper.Map<LoginResponseDto>(user);
        }


        public Task LogoutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new ValidationException("HTTP context is unavailable.");

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
