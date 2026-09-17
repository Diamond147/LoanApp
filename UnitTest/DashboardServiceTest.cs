using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.DTOs.Users.ResponseDto;
using Moq;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Application.Exceptions;

namespace UnitTest
{
    public class DashboardServiceTest
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly DashboardService _service;

        public DashboardServiceTest()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockCacheService = new Mock<ICacheService>();
            _mockMapper = new Mock<IMapper>();

            _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<UserProfile>()))
                .Returns((UserProfile u) => new UserProfileDto { Id = u.Id, Email = u.Email });

            _mockMapper.Setup(m => m.Map<LoanDto>(It.IsAny<Loan>()))
                .Returns((Loan l) => new LoanDto { Id = l.Id });

            _mockMapper.Setup(m => m.Map<LoanHistoryDto>(It.IsAny<LoanHistory>()))
                .Returns((LoanHistory lh) => new LoanHistoryDto { Id = lh.Id });

            // cache passthrough
            _mockCacheService.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<LoanDashboardDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<LoanDashboardDto?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _service = new DashboardService(_mockUserRepo.Object, _mockCacheService.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetDashboardByIdAsync_UserNotFound_ThrowsNotFoundException()
        {
            _mockUserRepo.Setup(r => r.GetUserByIdAsync("noid")).ReturnsAsync((UserProfile?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetDashboardByIdAsync("noid"));
        }

        [Fact]
        public async Task GetDashboardByIdAsync_ReturnsDashboard()
        {
            var user = new UserProfile { Id = "u1", Email = "u1@x.com", Loans = new List<Loan> { new Loan { Id = "l1", LoanHistories = new List<LoanHistory> { new LoanHistory { Id = "h1" } } } } };
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _service.GetDashboardByIdAsync(user.Id);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.User!.Id);
            Assert.Single(result.Loans);
            Assert.Single(result.LoanHistory);
        }
    }
}
