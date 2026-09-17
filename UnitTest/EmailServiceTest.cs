using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using AutoMapper;
using Domain.DTOs.Emails;
using Domain.Entities;
using Moq;
using System.Threading;
using Xunit;

namespace UnitTest
{
    public class EmailServiceTest
    {
        private readonly Mock<IEmailClient> _mockClient;
        private readonly Mock<IEmailRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly EmailService _service;

        public EmailServiceTest()
        {
            _mockClient = new Mock<IEmailClient>();
            _mockRepo = new Mock<IEmailRepository>();
            _mockMapper = new Mock<IMapper>();

            _mockMapper.Setup(m => m.Map<EmailLog>(It.IsAny<EmailDto>()))
                .Returns((EmailDto dto) => new EmailLog { EmailAddress = dto.EmailAddress, Subject = dto.Subject });

            _service = new EmailService(_mockClient.Object, _mockRepo.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task SendEmailAsync_ClientReturnsTrue_LogsAndReturnsTrue()
        {
            _mockClient.Setup(c => c.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            _mockRepo.Setup(r => r.AddEmailLogAsync(It.IsAny<EmailLog>())).Returns(Task.CompletedTask);

            var dto = new EmailDto { EmailAddress = "a@b.com", Subject = "s", Body = "b", IsHtml = false };
            var result = await _service.SendEmailAsync(dto);

            Assert.True(result);
            _mockRepo.Verify(r => r.AddEmailLogAsync(It.Is<EmailLog>(l => l.EmailAddress == dto.EmailAddress)), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_ClientThrows_ExceptionHandledAndLogged_ReturnsFalse()
        {
            _mockClient.Setup(c => c.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new System.Exception("boom"));

            _mockRepo.Setup(r => r.AddEmailLogAsync(It.IsAny<EmailLog>())).Returns(Task.CompletedTask);

            var dto = new EmailDto { EmailAddress = "a@b.com", Subject = "s", Body = "b", IsHtml = false };
            var result = await _service.SendEmailAsync(dto);

            Assert.False(result);
            _mockRepo.Verify(r => r.AddEmailLogAsync(It.Is<EmailLog>(l => l.EmailAddress == dto.EmailAddress && l.IsSent == false)), Times.Once);
        }

        [Fact]
        public async Task SendLoanApprovalEmailAsync_UsesSendEmailAsync()
        {
            _mockClient.Setup(c => c.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);
            _mockRepo.Setup(r => r.AddEmailLogAsync(It.IsAny<EmailLog>())).Returns(Task.CompletedTask);

            var user = new UserProfile { Id = "u", Email = "u@x.com", FirstName = "F", LastName = "L" };
            var loan = new Loan { RequestedAmount = 500 };

            var result = await _service.SendLoanApprovalEmailAsync(user, loan);

            Assert.True(result);
            _mockRepo.Verify(r => r.AddEmailLogAsync(It.IsAny<EmailLog>()), Times.Once);
        }
    }
}
