using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.DTOs.Payments;
using Domain.Enums;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Http;
using Application.Exceptions;
using Application.Services.Interfaces.Services;

namespace UnitTest
{
    public class PaymentServiceTest
    {
        private readonly Mock<ILoanRepository> _mockLoanRepo;
        private readonly Mock<ILoanHistoryRepository> _mockLoanHistoryRepo;
        private readonly Mock<IPaymentRepository> _mockPaymentRepo;
        private readonly Mock<IPaystackClient> _mockPaystackClient;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IMapper> _mockMapper;

        private readonly PaymentService _service;

        public PaymentServiceTest()
        {
            _mockLoanRepo = new Mock<ILoanRepository>();
            _mockLoanHistoryRepo = new Mock<ILoanHistoryRepository>();
            _mockPaymentRepo = new Mock<IPaymentRepository>();
            _mockPaystackClient = new Mock<IPaystackClient>();
            _mockEmailService = new Mock<IEmailService>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockCacheService = new Mock<ICacheService>();
            _mockMapper = new Mock<IMapper>();

            // Mapper for pending payment -> PaymentResponseDto
            _mockMapper.Setup(m => m.Map<PaymentResponseDto>(It.IsAny<Payment>()))
                .Returns((Payment p) => new PaymentResponseDto { Reference = p.PaystackReference, Amount = p.Amount, LoanId = p.LoanId, AuthorizationUrl = p.AuthorizationUrl ?? string.Empty });

            _service = new PaymentService(
                _mockLoanRepo.Object,
                _mockLoanHistoryRepo.Object,
                _mockPaymentRepo.Object,
                _mockPaystackClient.Object,
                _mockEmailService.Object,
                _mockUserRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockCacheService.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task InitiatePaymentAsync_Unauthenticated_ThrowsUnauthorizedAccessException()
        {
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.InitiatePaymentAsync());
        }

        [Fact]
        public async Task InitiatePaymentAsync_UserNotFound_ThrowsNotFoundException()
        {
            var userId = "u1";
            var http = new DefaultHttpContext();
            http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("oid", userId), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "a@b.com") }, "test"));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(http);

            _mockUserRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((UserProfile?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.InitiatePaymentAsync());
        }

        [Fact]
        public async Task InitiatePaymentAsync_NoApprovedLoan_ThrowsNotFoundException()
        {
            var userId = "u2";
            var http = new DefaultHttpContext();
            http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("oid", userId), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "a@b.com") }, "test"));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(http);

            _mockUserRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(new UserProfile { Id = userId, Email = "a@b.com" });
            _mockLoanRepo.Setup(r => r.GetApprovedLoanByUserIdAsync(userId)).ReturnsAsync((Loan?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.InitiatePaymentAsync());
        }

        [Fact]
        public async Task InitiatePaymentAsync_AlreadyPaid_ThrowsValidationException()
        {
            var userId = "u3";
            var http = new DefaultHttpContext();
            http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("oid", userId), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "a@b.com") }, "test"));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(http);

            var loan = new Loan { Id = "loan1", PrincipalBalance = 0 };
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(new UserProfile { Id = userId, Email = "a@b.com" });
            _mockLoanRepo.Setup(r => r.GetApprovedLoanByUserIdAsync(userId)).ReturnsAsync(loan);

            _mockPaymentRepo.Setup(r => r.GetPaymentsByLoanIdAsync(loan.Id)).ReturnsAsync(new List<Payment> { new Payment { Status = PaymentStatus.Success } });

            await Assert.ThrowsAsync<ValidationException>(() => _service.InitiatePaymentAsync());
        }

        [Fact]
        public async Task InitiatePaymentAsync_PendingExists_ReturnsExistingPaymentDto()
        {
            var userId = "u4";
            var http = new DefaultHttpContext();
            http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("oid", userId), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "a@b.com") }, "test"));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(http);

            var loan = new Loan { Id = "loan2", PrincipalBalance = 100 };
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(new UserProfile { Id = userId, Email = "a@b.com" });
            _mockLoanRepo.Setup(r => r.GetApprovedLoanByUserIdAsync(userId)).ReturnsAsync(loan);

            var pending = new Payment { Id = "p1", Status = PaymentStatus.Pending, Amount = 150, PaystackReference = "ref1", AuthorizationUrl = "auth" };
            _mockPaymentRepo.Setup(r => r.GetPaymentsByLoanIdAsync(loan.Id)).ReturnsAsync(new List<Payment> { pending });

            var result = await _service.InitiatePaymentAsync();

            Assert.NotNull(result);
            Assert.Equal(pending.PaystackReference, result.Reference);
        }

        [Fact]
        public async Task InitiatePaymentAsync_Success_ReturnsPaymentResponse()
        {
            var userId = "u5";
            var http = new DefaultHttpContext();
            http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("oid", userId), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "a@b.com") }, "test"));
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(http);

            var loan = new Loan { Id = "loan3", PrincipalBalance = 200 };
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(new UserProfile { Id = userId, Email = "a@b.com" });
            _mockLoanRepo.Setup(r => r.GetApprovedLoanByUserIdAsync(userId)).ReturnsAsync(loan);
            _mockPaymentRepo.Setup(r => r.GetPaymentsByLoanIdAsync(loan.Id)).ReturnsAsync(new List<Payment>());

            // Mock CreatePaymentAsync to return the payment
            _mockPaymentRepo.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);

            // Mock paystack client response as JsonElement
            var json = JsonDocument.Parse("{\"data\":{\"authorization_url\":\"https://paystack.test/checkout\"}} ").RootElement;
            _mockPaystackClient.Setup(p => p.InitializeTransactionAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(json);

            _mockPaymentRepo.Setup(r => r.UpdatePaymentAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);

            var result = await _service.InitiatePaymentAsync();

            Assert.NotNull(result);
            Assert.Equal("https://paystack.test/checkout", result.AuthorizationUrl);
        }
    }
}
