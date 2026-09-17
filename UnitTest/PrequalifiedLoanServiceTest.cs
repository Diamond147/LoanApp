using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using AutoMapper;
using Domain.DTOs.Users.RequestDto;
using Domain.Entities;
using Domain.Enums;
using Moq;
using System.Threading;
using Xunit;
using Application.Exceptions;
using System.Collections.Generic;

namespace UnitTest
{
    public class PrequalifiedLoanServiceTest
    {
        private readonly Mock<IPrequalifiedLoanRepo> _mockRepo;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMapper> _mockMapper;
        private readonly PrequalifiedLoanService _service;

        public PrequalifiedLoanServiceTest()
        {
            _mockRepo = new Mock<IPrequalifiedLoanRepo>();
            _mockCache = new Mock<ICacheService>();
            _mockMapper = new Mock<IMapper>();

            _mockMapper.Setup(m => m.Map<PreQualifiedLoan>(It.IsAny<CreatePreQualifiedLoanDto>()))
                .Returns((CreatePreQualifiedLoanDto src) => new PreQualifiedLoan { LoanType = src.LoanType, MinAmount = src.MinAmount, MaxAmount = src.MaxAmount, InterestRate = src.InterestRate });

            _mockMapper.Setup(m => m.Map<Domain.DTOs.Users.ResponseDto.PreQualifiedLoanDto>(It.IsAny<PreQualifiedLoan>()))
                .Returns((PreQualifiedLoan src) => new Domain.DTOs.Users.ResponseDto.PreQualifiedLoanDto { Id = src.Id, LoanType = src.LoanType });

            // Cache pass-through where used
            _mockCache.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Domain.DTOs.Users.ResponseDto.PreQualifiedLoanDto>?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<List<Domain.DTOs.Users.ResponseDto.PreQualifiedLoanDto>?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _mockCache.Setup(cs => cs.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<Domain.DTOs.Users.ResponseDto.PreQualifiedLoanDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns<string, Func<Task<Domain.DTOs.Users.ResponseDto.PreQualifiedLoanDto?>>, TimeSpan?, CancellationToken>((k, cb, t, ct) => cb()!);

            _service = new PrequalifiedLoanService(_mockRepo.Object, _mockCache.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task CreatePreQualifiedLoanAsync_CallsRepoAndInvalidatesCache_ReturnsDto()
        {
            var dto = new CreatePreQualifiedLoanDto { LoanType = LoanType.Personal, MinAmount = 10, MaxAmount = 100, InterestRate = 0.1m };

            _mockRepo.Setup(r => r.AddPreQualifiedLoanAsync(It.IsAny<PreQualifiedLoan>())).Returns(Task.CompletedTask);

            var result = await _service.CreatePreQualifiedLoanAsync(dto);

            Assert.NotNull(result);
            _mockRepo.Verify(r => r.AddPreQualifiedLoanAsync(It.IsAny<PreQualifiedLoan>()), Times.Once);
            _mockCache.Verify(c => c.RemoveByPrefixAsync("prequalified:", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPreQualifiedLoansAsync_NoItems_ReturnsEmptyList()
        {
            _mockRepo.Setup(r => r.GetPreQualifiedLoansAsync(null, null)).ReturnsAsync(new List<PreQualifiedLoan>());

            var result = await _service.GetPreQualifiedLoansAsync(null, null);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPreQualifiedLoanByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetPreQualifiedLoanByIdAsync("nope")).ReturnsAsync((PreQualifiedLoan?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetPreQualifiedLoanByIdAsync("nope"));
        }

        [Fact]
        public async Task DeletePreQualifiedLoanAsync_NotFound_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.DeletePreQualifiedLoanAsync("noid")).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeletePreQualifiedLoanAsync("noid"));
        }
    }
}
