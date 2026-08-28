using Application.Exceptions;
using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
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
            _mockMapper.Setup(m => m.Map<Domain.Entities.UserProfile>(It.IsAny<Domain.DTOs.Users.RequestDto.CreateUserProfileDto>()))
                .Returns((Domain.DTOs.Users.RequestDto.CreateUserProfileDto src) => new Domain.Entities.UserProfile
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
        public async Task CreateUserProfileAsync_FirstUser_AssignsAdminRoleAndReturnsDto()
        {
            // Arrange
            var dto = new CreateUserProfileDto
            {
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                Password = "SecurePassword123"
            };

            _mockUserRepo
                .Setup(repo => repo.GetUserByEmailAsync(dto.Email))
                .ReturnsAsync((UserProfile?)null); // No existing user

            _mockUserRepo
                .Setup(repo => repo.AnyAsync())
                .ReturnsAsync(false); // First user in system

            // Act
            var result = await _authService.CreateUserProfileAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Email, result.Email);

            // Verify user saved with Admin role
            _mockUserRepo.Verify(repo => repo.AddUserAsync(
                It.Is<UserProfile>(u => u.Role == "Admin" && u.Email == dto.Email)), Times.Once);
        }

    }
}   
