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
    public class LoanHistoryServiceTest
    {
        private readonly Mock<ILoanHistoryRepository> _mockRepo;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMapper> _mockMapper;
        private readonly LoanHistoryService _service;

        public LoanHistoryServiceTest()
        {
            _mockRepo = new Mock<ILoanHistoryRepository>();
            _mockCache = new Mock<ICacheService>();
            _mockMapper = new Mock<IMapper>();

            _mockMapper.Setup(m => m.Map<LoanHistoryDto>(It.IsAny<LoanHistory>()))
                .Returns((LoanHistory h) => new LoanHistoryDto { Id = h.Id });

            // The cache's GetOrSetAsync is generic; when the service's callback returns a List<LoanHistoryDto>
            // the inferred T is List<LoanHistoryDto>, so the mock must match that exact generic signature.
            _mockCache.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<LoanHistoryDto>>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<List<LoanHistoryDto>>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _mockCache.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<LoanHistoryDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<LoanHistoryDto?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _service = new LoanHistoryService(_mockRepo.Object, _mockCache.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetLoanHistoryByLoanIdAsync_ReturnsMappedList()
        {
            var history = new LoanHistory { Id = "h1", LoanId = "l1" };
            _mockRepo.Setup(r => r.GetLoanHistoryByLoanIdAsync("l1")).ReturnsAsync(new List<LoanHistory> { history });

            var list = await _service.GetLoanHistoryByLoanIdAsync("l1");

            Assert.NotNull(list);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetLoanHistoryByHistoryIdAsync_NotFound_ReturnsNull()
        {
            _mockRepo.Setup(r => r.GetLoanHistoryByHistoryIdAsync("noid")).ReturnsAsync((LoanHistory?)null);

            var dto = await _service.GetLoanHistoryByHistoryIdAsync("noid");

            Assert.Null(dto);
        }

        [Fact]
        public async Task DeleteLoanHistoryAsync_NotFound_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.DeleteLoanHistoryAsync("noid")).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteLoanHistoryAsync("noid"));
        }
    }
}
