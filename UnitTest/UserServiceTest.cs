using Application.DTOs;
using Application.Exceptions;
using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Threading;
using System.Security.Claims;
using Xunit;
using Domain.DTOs.Users.RequestDto;

namespace UnitTest
{
    public class UserServiceTest
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<AutoMapper.IMapper> _mockMapper;

        private readonly UserService _userService;

        public UserServiceTest()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockCacheService = new Mock<ICacheService>();
            _mockMapper = new Mock<AutoMapper.IMapper>();

            // Default mapping behavior
            _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<UserProfile>()))
                .Returns((UserProfile src) => new UserProfileDto
                {
                    Id = src.Id,
                    FirstName = src.FirstName,
                    LastName = src.LastName,
                    Email = src.Email,
                    MobileNumber = src.MobileNumber,
                    Nationality = src.Nationality,
                    DateOfBirth = src.DateOfBirth,
                    SignUpDate = src.SignUpDate
                });

            _mockMapper.Setup(m => m.Map<AllUserDetailsDto>(It.IsAny<UserProfile>()))
                .Returns((UserProfile src) => new AllUserDetailsDto
                {
                    Id = src.Id,
                    FirstName = src.FirstName,
                    LastName = src.LastName,
                    Email = src.Email
                });

            _mockMapper.Setup(m => m.Map<LoanDto>(It.IsAny<Loan>()))
                .Returns((Loan src) => new LoanDto { Id = src.Id, RequestedDate = src.RequestedDate });

            _mockMapper.Setup(m => m.Map<LoanHistoryDto>(It.IsAny<LoanHistory>()))
                .Returns((LoanHistory src) => new LoanHistoryDto { Id = src.Id, RequestedDate = src.RequestedDate });

            // Cache pass-through: forces the callback to run so we're really testing service logic, not the cache
            _mockCacheService.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<ContinuationResponse<UserProfileDto>?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<ContinuationResponse<UserProfileDto>?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _mockCacheService.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<ContinuationResponse<AllUserDetailsDto>?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<ContinuationResponse<AllUserDetailsDto>?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _mockCacheService.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<UserProfileDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<UserProfileDto?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _userService = new UserService(
                _mockUserRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockCacheService.Object,
                _mockMapper.Object
            );
        }


        // GET-ALL-USERS

        [Fact]
        public async Task GetAllUsersAsync_InvalidPageSize_ThrowsValidationException()
        {
            // Arrange - pageSize of 0 breaks the "1 to 100" rule
            int invalidPageSize = 0;

            // Act & Assert - method should reject it before touching the repository at all
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _userService.GetAllUsersAsync(invalidPageSize, null, null));

            Assert.Equal("Page size must be between 1 and 100.", ex.Message);
        }


        [Fact]
        public async Task GetAllUsersAsync_ReturnsMappedUsers()
        {
            // Arrange - repository returns one user; cache setup (constructor) lets the real logic run
            var user = new UserProfile { Id = "u1", FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

            _mockUserRepo.Setup(r => r.GetAllUsersAsync(10, null, null))
                .ReturnsAsync((new List<UserProfile> { user }, (string?)null));

            // Act
            var result = await _userService.GetAllUsersAsync(10, null, null);

            // Assert - the one user came back, correctly mapped
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal(user.Email, result.Data[0].Email);
        }


        // GET-ALL-USERS-DETAILS 

        [Fact]
        public async Task GetAllUsersDetailsAsync_InvalidPageSize_ThrowsValidationException()
        {
            // Arrange - pageSize of 101 is above the allowed max
            int invalidPageSize = 101;

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                _userService.GetAllUsersDetailsAsync(invalidPageSize, null, null, null, null, null, null, null));

            Assert.Equal("PageSize must be between 1 and 100.", ex.Message);
        }


        [Fact]
        public async Task GetAllUsersDetailsAsync_NoUsersFound_ReturnsEmptyResult()
        {
            // Arrange - repository finds nobody matching the filters
            _mockUserRepo.Setup(r => r.GetAllUsersDetailsAsync(10, null, null, null, null, null, null, null))
                .ReturnsAsync((new List<UserProfile>(), (string?)null));

            // Act
            var result = await _userService.GetAllUsersDetailsAsync(10, null, null, null, null, null, null, null);

            // Assert - empty list, not null, and no "more pages" flag
            Assert.NotNull(result);
            Assert.Empty(result.Data);
            Assert.False(result.HasMore);
        }


        [Fact]
        public async Task GetAllUsersDetailsAsync_ReturnsUserWithLoansAndHistories()
        {
            // Arrange - build one user carrying one loan, which itself carries one history entry
            var loanHistory = new LoanHistory { Id = "lh1", LoanId = "l1", RequestedDate = DateTime.UtcNow };
            var loan = new Loan { Id = "l1", RequestedDate = DateTime.UtcNow, LoanHistories = new List<LoanHistory> { loanHistory } };
            var user = new UserProfile { Id = "u1", Email = "jane@example.com", Loans = new List<Loan> { loan } };

            _mockUserRepo.Setup(r => r.GetAllUsersDetailsAsync(10, null, null, null, null, null, null, null))
                .ReturnsAsync((new List<UserProfile> { user }, (string?)null));

            // Act
            var result = await _userService.GetAllUsersDetailsAsync(10, null, null, null, null, null, null, null);

            // Assert - the nested Loans and LoanHistories collections were flattened and mapped correctly
            var userDetail = Assert.Single(result.Data);

            Assert.Single(userDetail.Loans);
            Assert.Single(userDetail.LoanHistories);
        }


        // CHANGE-ROLE

        [Fact]
        public async Task ChangeUserRoleAsync_InvalidRole_ThrowsArgumentException()
        {
            // Arrange - "superuser" isn't Admin or User
            var dto = new ChangeRoleDto { NewRole = "superuser" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _userService.ChangeUserRoleAsync("someId", dto));

            Assert.Contains("Invalid role", ex.Message);
        }


        [Fact]
        public async Task ChangeUserRoleAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange - repo has nobody with this ID
            var dto = new ChangeRoleDto { NewRole = "User" };
            _mockUserRepo.Setup(r => r.GetUserByIdAsync("missing")).ReturnsAsync((UserProfile?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.ChangeUserRoleAsync("missing", dto));
        }


        [Fact]
        public async Task ChangeUserRoleAsync_Success_UpdatesRoleAndInvalidatesCache()
        {
            // Arrange - existing "User" being promoted to "Admin"
            var user = new UserProfile { Id = "u2", Role = "User", Email = "u2@example.com" };
            var dto = new ChangeRoleDto { NewRole = "Admin" };

            _mockUserRepo.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.UpdateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync((UserProfile u) => u);

            // Act
            await _userService.ChangeUserRoleAsync(user.Id, dto);

            // Assert - role was saved as Admin, and all 3 related caches were cleared
            _mockUserRepo.Verify(r => r.UpdateUserAsync(It.Is<UserProfile>(u => u.Role == "Admin" && u.Id == user.Id)), Times.Once);

            _mockCacheService.Verify(c => c.RemoveAsync($"users:id:{user.Id}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveByPrefixAsync("users:all:", It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveByPrefixAsync("users:details:", It.IsAny<CancellationToken>()), Times.Once);
        }


        // GET-BY-ID

        [Fact]
        public async Task GetUserByIdAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange - repo has nobody with this ID (cache pass-through already set in constructor)
            _mockUserRepo.Setup(r => r.GetUserByIdAsync("nope")).ReturnsAsync((UserProfile?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserByIdAsync("nope"));
        }


        [Fact]
        public async Task GetUserByIdAsync_UserFound_ReturnsMappedDto()
        {
            // Arrange
            var user = new UserProfile { Id = "u3", Email = "found@example.com" };

            _mockUserRepo.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserByIdAsync(user.Id);

            // Assert - correct user came back, correctly mapped
            Assert.NotNull(result);
            Assert.Equal(user.Email, result!.Email);
        }


        // UPDATE

        [Fact]
        public async Task UpdateUserAsync_Unauthenticated_ThrowsUnauthorizedAccessException()
        {
            // Arrange - no logged-in user on the request at all
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _userService.UpdateUserAsync(new UpdateUserProfileDto()));
        }


        [Fact]
        public async Task UpdateUserAsync_UserProfileMissing_ThrowsNotFoundException()
        {
            // Arrange - token identifies a user ID that no longer exists in the DB
            var userId = "ghost-user";
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            _mockUserRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((UserProfile?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.UpdateUserAsync(new UpdateUserProfileDto()));
        }


        [Fact]
        public async Task UpdateUserAsync_Success_UpdatesAndReturnsDto()
        {
            // Arrange - authenticated user updates their mobile number
            var userId = "current-user";
            var existingUser = new UserProfile { Id = userId, MobileNumber = "123" };

            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            _mockUserRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserRepo.Setup(r => r.UpdateUserAsync(It.IsAny<UserProfile>())).ReturnsAsync((UserProfile u) => u);

            // Act
            var result = await _userService.UpdateUserAsync(new UpdateUserProfileDto { MobileNumber = "999" });

            // Assert - saved value AND returned DTO both reflect the change; cache invalidated
            Assert.Equal("999", result.MobileNumber);

            _mockUserRepo.Verify(r => r.UpdateUserAsync(It.Is<UserProfile>(u => u.MobileNumber == "999")), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync($"users:id:{userId}", It.IsAny<CancellationToken>()), Times.Once);
        }


        // PATCH

        [Fact]
        public async Task PatchUserAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _mockUserRepo.Setup(r => r.GetUserByIdAsync("missing")).ReturnsAsync((UserProfile?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.PatchUserAsync("missing", new PatchUserProfileDto()));
        }


        [Fact]
        public async Task PatchUserAsync_Success_UpdatesOnlyProvidedFields()
        {
            // Arrange - existing user has a FirstName; patch only changes LastName, FirstName should stay untouched
            var user = new UserProfile { Id = "u4", FirstName = "OriginalFirst", LastName = "OldLast" };
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.PatchUserAsync(It.IsAny<UserProfile>())).ReturnsAsync((UserProfile u) => u);

            // Act
            var result = await _userService.PatchUserAsync(user.Id, new PatchUserProfileDto { LastName = "NewLast" });

            // Assert - only LastName changed; FirstName left as-is; caches cleared
            Assert.Equal("OriginalFirst", result.FirstName);
            Assert.Equal("NewLast", result.LastName);
            _mockCacheService.Verify(c => c.RemoveAsync($"users:id:{user.Id}", It.IsAny<CancellationToken>()), Times.Once);
        }


        // DELETE

        [Fact]
        public async Task DeleteUserAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange - repository's own delete method reports nothing was found/deleted
            _mockUserRepo.Setup(r => r.DeleteUserAsync("missing")).ReturnsAsync((UserProfile?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.DeleteUserAsync("missing"));
        }


        [Fact]
        public async Task DeleteUserAsync_Success_ReturnsTrueAndInvalidatesCache()
        {
            // Arrange
            var user = new UserProfile { Id = "u5" };
            _mockUserRepo.Setup(r => r.DeleteUserAsync(user.Id)).ReturnsAsync(user);

            // Act
            var result = await _userService.DeleteUserAsync(user.Id);

            // Assert - confirms success flag AND that all related caches were cleared
            Assert.True(result);
            _mockCacheService.Verify(c => c.RemoveAsync($"users:id:{user.Id}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveByPrefixAsync("users:all:", It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveByPrefixAsync("users:details:", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}