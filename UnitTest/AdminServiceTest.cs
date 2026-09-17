using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Moq;
using System.Threading;
using Xunit;
using Application.Exceptions;

namespace UnitTest
{
    public class AdminServiceTest
    {
        private readonly Mock<IAdminRepository> _mockRepo;
        private readonly Mock<ICacheService> _mockCache;
        private readonly AdminService _service;

        public AdminServiceTest()
        {
            _mockRepo = new Mock<IAdminRepository>();
            _mockCache = new Mock<ICacheService>();

            _service = new AdminService(_mockRepo.Object, _mockCache.Object);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_NotFound_ThrowsNotFoundException()
        {
            _mockCache.Setup(c => c.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<Domain.Entities.AdminDashboardStats?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<Domain.Entities.AdminDashboardStats?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb());

            _mockRepo.Setup(r => r.GetDashboardStatsAsync()).ReturnsAsync((Domain.Entities.AdminDashboardStats?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetDashboardStatsAsync());
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ReturnsStats()
        {
            var stats = new Domain.Entities.AdminDashboardStats { TotalLoans = 5 };
            _mockCache.Setup(c => c.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<Domain.Entities.AdminDashboardStats?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<Domain.Entities.AdminDashboardStats?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _mockRepo.Setup(r => r.GetDashboardStatsAsync()).ReturnsAsync(stats);

            var result = await _service.GetDashboardStatsAsync();

            Assert.NotNull(result);
            Assert.Equal(5, result.TotalLoans);
        }
    }
}
