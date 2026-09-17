using Application.DTOs;
using Application.Exceptions;
using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using System.Threading;
using Xunit;

namespace UnitTest
{
    // LoanService tests follow the established pattern used in AuthServiceTest and UserServiceTest:
    // - Arrange dependencies with Moq
    // - Use cache GetOrSet pass-through to exercise service logic
    // - Assert expected exceptions, repository calls, cache invalidation and email notifications
    public class LoanServiceTest
    {
        private readonly Mock<ILoanRepository> _mockLoanRepo;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILoanHistoryRepository> _mockLoanHistoryRepo;
        private readonly Mock<IPrequalifiedLoanRepo> _mockPrequalifiedRepo;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<AutoMapper.IMapper> _mockMapper;

        private readonly LoanService _loanService;

        public LoanServiceTest()
        {
            _mockLoanRepo = new Mock<ILoanRepository>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLoanHistoryRepo = new Mock<ILoanHistoryRepository>();
            _mockPrequalifiedRepo = new Mock<IPrequalifiedLoanRepo>();
            _mockCacheService = new Mock<ICacheService>();
            _mockMapper = new Mock<AutoMapper.IMapper>();

            // Mapping behaviours used across tests
            _mockMapper.Setup(m => m.Map<Loan>(It.IsAny<CreateLoanDto>()))
                .Returns((CreateLoanDto src) => new Loan { RequestedAmount = src.RequestedAmount, LoanType = src.loanType, RequestedDate = DateTime.UtcNow });

            _mockMapper.Setup(m => m.Map<LoanDto>(It.IsAny<Loan>()))
                .Returns((Loan src) => new LoanDto { Id = src.Id, RequestedDate = src.RequestedDate, UserProfileId = src.UserProfileId, RequestedAmount = src.RequestedAmount, AccruedInterest = src.AccruedInterest, PrincipalBalance = src.PrincipalBalance, Status = src.Status });

            _mockMapper.Setup(m => m.Map<LoanHistory>(It.IsAny<Loan>()))
                .Returns((Loan src) => new LoanHistory { LoanId = src.Id, RequestedDate = src.RequestedDate, RequestedAmount = src.RequestedAmount, InterestRate = src.InterestRate, AccruedInterest = src.AccruedInterest, PrincipalBalance = src.PrincipalBalance, Status = src.Status, UserProfileId = src.UserProfileId });

            // Cache pass-through: force the getItemCallback to run so service logic executes
            _mockCacheService.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<ContinuationResponse<LoanDto>?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<ContinuationResponse<LoanDto>?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _mockCacheService.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<LoanDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<LoanDto?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _loanService = new LoanService(
                _mockLoanRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockUserRepo.Object,
                _mockEmailService.Object,
                _mockLoanHistoryRepo.Object,
                _mockPrequalifiedRepo.Object,
                _mockCacheService.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task CreateLoanAsync_Unauthenticated_ThrowsUnauthorizedAccessException()
        {
            // Arrange - no HttpContext or user claims
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _loanService.CreateLoanAsync(new CreateLoanDto { loanType = LoanType.Personal, RequestedAmount = 100 }));
        }

        [Fact]
        public async Task CreateLoanAsync_HasUnpaidLoan_ThrowsValidationException()
        {
            // Arrange - authenticated user but has unpaid loan
            var userId = "user-1";
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("oid", userId), new Claim(ClaimTypes.Email, "a@b.com") }));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            _mockLoanRepo.Setup(r => r.HasUnpaidLoanAsync(userId)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _loanService.CreateLoanAsync(new CreateLoanDto { loanType = LoanType.Personal, RequestedAmount = 100 }));
        }

        [Fact]
        public async Task CreateLoanAsync_LoanTypeNotFound_ThrowsNotFoundException()
        {
            // Arrange - authenticated user, no unpaid loans, but prequalified not found
            var userId = "user-2";
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("oid", userId), new Claim(ClaimTypes.Email, "a@b.com") }));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            _mockLoanRepo.Setup(r => r.HasUnpaidLoanAsync(userId)).ReturnsAsync(false);
            _mockPrequalifiedRepo.Setup(p => p.GetPreQualifiedLoanByTypeAsync(LoanType.Personal)).ReturnsAsync((PreQualifiedLoan?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _loanService.CreateLoanAsync(new CreateLoanDto { loanType = LoanType.Personal, RequestedAmount = 100 }));
        }

        [Fact]
        public async Task CreateLoanAsync_InvalidAmount_ThrowsValidationException()
        {
            // Arrange - prequalified loan exists but requested amount outside allowed range
            var userId = "user-3";
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("oid", userId), new Claim(ClaimTypes.Email, "a@b.com") }));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            _mockLoanRepo.Setup(r => r.HasUnpaidLoanAsync(userId)).ReturnsAsync(false);

            _mockPrequalifiedRepo.Setup(p => p.GetPreQualifiedLoanByTypeAsync(LoanType.Personal))
                .ReturnsAsync(new PreQualifiedLoan { LoanType = LoanType.Personal, MinAmount = 500, MaxAmount = 1000, InterestRate = 0.1m });

            // RequestedAmount less than MinAmount
            await Assert.ThrowsAsync<ValidationException>(() => _loanService.CreateLoanAsync(new CreateLoanDto { loanType = LoanType.Personal, RequestedAmount = 100 }));
        }

        [Fact]
        public async Task CreateLoanAsync_Success_AddsLoanHistoryAndInvalidatesCache_ReturnsDto()
        {
            // Arrange - happy path
            var userId = "user-4";
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("oid", userId), new Claim(ClaimTypes.Email, "a@b.com") }));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            _mockLoanRepo.Setup(r => r.HasUnpaidLoanAsync(userId)).ReturnsAsync(false);

            _mockPrequalifiedRepo.Setup(p => p.GetPreQualifiedLoanByTypeAsync(LoanType.Personal))
                .ReturnsAsync(new PreQualifiedLoan { LoanType = LoanType.Personal, MinAmount = 50, MaxAmount = 1000, InterestRate = 0.15m });

            // Verify that AddLoanAsync and AddLoanHistoryAsync are called
            _mockLoanRepo.Setup(r => r.AddLoanAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);
            _mockLoanHistoryRepo.Setup(r => r.AddLoanHistoryAsync(It.IsAny<LoanHistory>())).Returns(Task.CompletedTask);

            // Act
            var createDto = new CreateLoanDto { loanType = LoanType.Personal, RequestedAmount = 100 };
            var result = await _loanService.CreateLoanAsync(createDto);

            // Assert
            Assert.NotNull(result);

            _mockLoanRepo.Verify(r => r.AddLoanAsync(It.IsAny<Loan>()), Times.Once);
            _mockLoanHistoryRepo.Verify(r => r.AddLoanHistoryAsync(It.IsAny<LoanHistory>()), Times.Once);

            _mockCacheService.Verify(c => c.RemoveAsync($"loans:user:{userId}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveByPrefixAsync("loans:all:", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllLoansAsync_InvalidPageSize_ThrowsNotFoundException()
        {
            // Act & Assert - page size outside allowed range
            await Assert.ThrowsAsync<NotFoundException>(() => _loanService.GetAllLoansAsync(0, null, null, null));
        }

        [Fact]
        public async Task GetAllLoansAsync_ReturnsMappedLoans()
        {
            // Arrange
            var loan = new Loan { Id = "l1", RequestedDate = DateTime.UtcNow, RequestedAmount = 200 };
            
            _mockLoanRepo.Setup(r => r.GetAllLoansAsync(10, null, null, null)).ReturnsAsync((new List<Loan> { loan }, (string?)null));

            // Act
            var result = await _loanService.GetAllLoansAsync(10, null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal(loan.RequestedAmount, result.Data[0].RequestedAmount);
        }

        [Fact]
        public async Task GetLoanByIdAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange - cache factory will call repository which returns null
            _mockLoanRepo.Setup(r => r.GetLoanByIdAsync("missing")).ReturnsAsync((Loan?)null);
            // Ensure cache callback executes
            _mockCacheService.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<LoanDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<LoanDto?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            await Assert.ThrowsAsync<NotFoundException>(() => _loanService.GetLoanByIdAsync("missing", "user"));
        }

        [Fact]
        public async Task GetLoanByIdAsync_Unauthorized_ThrowsUnauthorizedException()
        {
            // Arrange - repository returns loan owned by another user
            var loan = new Loan { Id = "l2", RequestedDate = DateTime.UtcNow, UserProfileId = "owner" };
            _mockLoanRepo.Setup(r => r.GetLoanByIdAsync(loan.Id)).ReturnsAsync(loan);
            _mockCacheService.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<LoanDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<LoanDto?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            await Assert.ThrowsAsync<UnauthorizedException>(() => _loanService.GetLoanByIdAsync(loan.Id, "different-user"));
        }

        [Fact]
        public async Task UpdateLoanStatusAsync_NoChange_ReturnsMappedLoanWithoutSideEffects()
        {
            // Arrange - loan exists and status equals requested new status
            var loan = new Loan { Id = "l3", Status = LoanStatus.Pending, UserProfileId = "u" };
            _mockLoanRepo.Setup(r => r.GetLoanByIdAsync(loan.Id)).ReturnsAsync(loan);
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(loan.UserProfileId)).ReturnsAsync(new UserProfile { Id = loan.UserProfileId, Email = "a@b.com" });

            var result = await _loanService.UpdateLoanStatusAsync(loan.Id, new UpdateLoanStatusDto { NewStatus = LoanStatus.Pending });

            Assert.NotNull(result);
            _mockLoanRepo.Verify(r => r.UpdateLoanAsync(It.IsAny<Loan>()), Times.Never);
            _mockLoanHistoryRepo.Verify(r => r.AddLoanHistoryAsync(It.IsAny<LoanHistory>()), Times.Never);
        }

        [Fact]
        public async Task UpdateLoanStatusAsync_Approve_SendsEmailAndUpdatesHistory()
        {
            // Arrange - loan exists and will be approved
            var loan = new Loan { Id = "l4", Status = LoanStatus.Pending, UserProfileId = "user-x", RequestedAmount = 300, PrincipalBalance = 300 };
            _mockLoanRepo.Setup(r => r.GetLoanByIdAsync(loan.Id)).ReturnsAsync(loan);
            _mockPrequalifiedRepo.Setup(p => p.GetPreQualifiedLoanByTypeAsync(loan.LoanType)).ReturnsAsync(new PreQualifiedLoan { InterestRate = 0.2m });
            _mockLoanRepo.Setup(r => r.UpdateLoanAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);
            _mockLoanHistoryRepo.Setup(r => r.AddLoanHistoryAsync(It.IsAny<LoanHistory>())).Returns(Task.CompletedTask);
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(loan.UserProfileId)).ReturnsAsync(new UserProfile { Id = loan.UserProfileId, Email = "user@example.com" });
            _mockEmailService.Setup(e => e.SendLoanApprovalEmailAsync(It.IsAny<UserProfile>(), It.IsAny<Loan>())).ReturnsAsync(true);

            var result = await _loanService.UpdateLoanStatusAsync(loan.Id, new UpdateLoanStatusDto { NewStatus = LoanStatus.Approved });

            Assert.NotNull(result);
            _mockLoanRepo.Verify(r => r.UpdateLoanAsync(It.IsAny<Loan>()), Times.Once);
            _mockLoanHistoryRepo.Verify(r => r.AddLoanHistoryAsync(It.IsAny<LoanHistory>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync($"loans:id:{loan.Id}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync($"loans:user:{loan.UserProfileId}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveByPrefixAsync("loans:all:", It.IsAny<CancellationToken>()), Times.Once);
            _mockEmailService.Verify(e => e.SendLoanApprovalEmailAsync(It.IsAny<UserProfile>(), It.IsAny<Loan>()), Times.Once);
        }

        [Fact]
        public async Task DeleteLoanAsync_NotFound_ThrowsNotFoundException()
        {
            _mockLoanRepo.Setup(r => r.GetLoanByIdAsync("noid")).ReturnsAsync((Loan?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _loanService.DeleteLoanAsync("noid"));
        }

        [Fact]
        public async Task DeleteLoanAsync_Success_DeletesAndInvalidatesCache_ReturnsTrue()
        {
            var loan = new Loan { Id = "l5", UserProfileId = "user-y" };
            _mockLoanRepo.Setup(r => r.GetLoanByIdAsync(loan.Id)).ReturnsAsync(loan);
            _mockLoanRepo.Setup(r => r.DeleteLoanAsync(loan.Id)).ReturnsAsync(true);

            var result = await _loanService.DeleteLoanAsync(loan.Id);

            Assert.True(result);
            _mockCacheService.Verify(c => c.RemoveAsync($"loans:id:{loan.Id}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync($"loans:user:{loan.UserProfileId}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveByPrefixAsync("loans:all:", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
