using Application.Exceptions;
using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace UnitTest
{
    public class AuthServiceTest
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<AutoMapper.IMapper> _mockMapper;

        private readonly AuthService _authService;

        public AuthServiceTest()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockTokenService = new Mock<ITokenService>();
            _mockMapper = new Mock<AutoMapper.IMapper>();

            // Default mapping behavior for CreateUserProfileDto -> UserProfile used in tests
            _mockMapper.Setup(m => m.Map<UserProfile>(It.IsAny<CreateUserProfileDto>()))
                .Returns((CreateUserProfileDto src) => new UserProfile
                {
                    FirstName = src.FirstName,
                    LastName = src.LastName,
                    Email = src.Email,
                    Gender = src.Gender,
                    DateOfBirth = src.DateOfBirth,
                    MobileNumber = src.MobileNumber,
                    Nationality = src.Nationality
                });

            _authService = new AuthService(
                _mockUserRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockTokenService.Object,
                _mockMapper.Object
            );
        }


        [Fact]
        public async Task CreateUserProfileAsync_EmailAlreadyExists_ThrowsConflictException()
        {
            // Arrange - Only Email is necessary for this execution path
            var Dto = new CreateUserProfileDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
            };

            var ExistingUser = new UserProfile
            {
                Id = "ExistingUser",
                FirstName = "Existing",
                LastName = "User",
                Email = Dto.Email
            };

            _mockUserRepo.Setup(repo => repo.GetUserByEmailAsync(Dto.Email))
                .ReturnsAsync(ExistingUser);

            // Act & Assert
            var result = await Assert.ThrowsAsync<ConflictException>(() => _authService.CreateUserProfileAsync(Dto));

            Assert.NotNull(result);
            Assert.Equal("A user with this email already exists.", result.Message);

            // Verify repository Add was NEVER called
            _mockUserRepo.Verify(repo => repo.AddUserAsync(It.IsAny<UserProfile>()), Times.Never);
        }


        [Fact]
        public async Task CreateUserProfileAsync_FirstUser_AssignsAdminRole()
        {
            // Arrange
            var dto = new CreateUserProfileDto
            {
                Email = "jane.doe@example.com",
                FirstName = "Jane",
                LastName = "Doe",
                Password = "SecurePassword123"
            };

            _mockUserRepo
                .Setup(repo => repo.GetUserByEmailAsync(dto.Email))
                .ReturnsAsync((UserProfile?)null); // No existing user

            _mockUserRepo
                .Setup(repo => repo.AnyAsync())
                .ReturnsAsync(false); // First user in system

            _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<UserProfile>()))
                .Returns(new UserProfileDto { Email = dto.Email });

            // Act
            var result = await _authService.CreateUserProfileAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Email, result.Email);

            // Verify user saved with Admin role
            _mockUserRepo.Verify(repo => repo.AddUserAsync(
                It.Is<UserProfile>(u => u.Role == "Admin" && u.Email == dto.Email)), Times.Once);
        }


        [Fact]
        public async Task CreateUserProfileAsync_NotFirstUser_AssignUserRole()
        {
            // Arrange
            var dto = new CreateUserProfileDto
            {
                Email = "johndoe@example.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "SecurePassword123"
            };

            _mockUserRepo
                .Setup(repo => repo.GetUserByEmailAsync(dto.Email))
                .ReturnsAsync((UserProfile?)null); // No existing user

            _mockUserRepo
                .Setup(repo => repo.AnyAsync())
                .ReturnsAsync(true); // Not the first user in the system

            _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<UserProfile>()))
                .Returns(new UserProfileDto { Email = dto.Email });

            // Act
            var result = await _authService.CreateUserProfileAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Email, result.Email);

            // Verify user saved with User role
            _mockUserRepo.Verify(repo => repo.AddUserAsync(
                It.Is<UserProfile>(u => u.Role == "User" && u.Email == dto.Email)), Times.Once);
        }


        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsValidationException()
        {
            // Arrange - no user exists with this email
            var request = new LoginRequestDto
            {
                Email = "wrong@example.com",
                Password = "password"
            };

            _mockUserRepo.Setup(repo => repo.GetUserByEmailAsync(request.Email))
            .ReturnsAsync((UserProfile?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _authService.LoginAsync(request));
        }


        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsValidationException()
        {
            // Arrange - create a REAL bcrypt hash for "CorrectPassword",
            // then try logging in with a DIFFERENT password to force a mismatch
            var correctHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");

            var existingUser = new UserProfile
            {
                Id = "User1",
                Email = "User@example.com",
                PasswordHash = correctHash
            };

            var request = new LoginRequestDto
            {
                Email = existingUser.Email,
                Password = "WrongPassword"
            };

            _mockUserRepo.Setup(repo => repo.GetUserByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _authService.LoginAsync(request));
        }


        [Fact]
        public async Task LoginAsync_ValidCredentails_ReturnsDtoAndSetsCookie()
        {
            // Arrange - real hash + matching password so VerifyPassword succeeds
            var correctPassword = "CorrectPassword";

            var existingUser = new UserProfile
            {
                Id = "User1",
                Email = "correct@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword),
                Role = "User"
            };

            var request = new LoginRequestDto
            {
                Email = existingUser.Email,
                Password = correctPassword
            };

            _mockUserRepo.Setup(repo => repo.GetUserByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            _mockTokenService.Setup(t => t.GenerateAccessToken(existingUser.Id, existingUser.Email, It.IsAny<String[]>()))
                .Returns("fake-jwt-token");

            // HttpContext can't be easily mocked with Moq (it's not fully virtual),
            // so we use a real DefaultHttpContext instead - lightweight and works fine for cookie checks
            var httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            _mockMapper.Setup(m => m.Map<LoginResponseDto>(existingUser))
                .Returns(new LoginResponseDto { Email = existingUser.Email });

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert -correct DTO returned
            Assert.NotNull(result);
            Assert.Equal(existingUser.Email, result.Email);

            // Assert - the JWT cookie was actually appended to the response
            Assert.True(httpContext.Response.Headers.ContainsKey("Set-Cookie"));
            Assert.Contains("X-Access-Token", httpContext.Response.Headers["Set-Cookie"].ToString());
        }


        [Fact]
        public async Task LogoutAsync_ExpiresAccessTokenCookie()
        {
            // Arrange - real HttpContext so we can inspect the Set-Cookie header afterward
            var httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            // Act
            await _authService.LogoutAsync();

            // Assert - a cookie named X-Access-Token was written (with an expired date, clearing it)
            Assert.True(httpContext.Response.Headers.ContainsKey("Set-Cookie"));
            Assert.Contains("X-Access-Token", httpContext.Response.Headers["Set-Cookie"].ToString());
        }

    }
}   
