using Application.Services.Implementations;
using Application.Services.Interfaces.Services;
using Moq;
using Microsoft.Extensions.Configuration;
using Xunit;
using Domain.DTOs.Payments;
using System.Threading.Tasks;

namespace UnitTest
{
    public class PaystackWebhookTest
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly PaystackWebhook _webhook;

        public PaystackWebhookTest()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockPaymentService = new Mock<IPaymentService>();

            _webhook = new PaystackWebhook(_mockConfig.Object, _mockPaymentService.Object);
        }

        [Fact]
        public async Task PaystackWebhookAsync_EmptyBody_ReturnsTrue()
        {
            var payload = new PaystackWebhookDto { Event = "charge.success", Data = new PaystackWebhookData() };
            var result = await _webhook.PaystackWebhookAsync(payload, string.Empty, "sig");

            Assert.True(result);
        }

        [Fact]
        public async Task PaystackWebhookAsync_InvalidSignature_ReturnsFalse()
        {
            _mockConfig.Setup(c => c["Paystack:WebhookSecret"]).Returns("secret");

            var payload = new PaystackWebhookDto { Event = "charge.success", Data = new PaystackWebhookData() };
            // Provide mismatched signature
            var result = await _webhook.PaystackWebhookAsync(payload, "{\"a\":1}", "bad-signature");

            Assert.False(result);
        }

        [Fact]
        public async Task PaystackWebhookAsync_ChargeSuccess_CallsProcessSuccessfulWebhookAsync_ReturnsTrue()
        {
            // Arrange
            var secret = "test-secret";
            _mockConfig.Setup(c => c["Paystack:WebhookSecret"]).Returns(secret);

            var data = new PaystackWebhookData { Reference = "ref", Amount = 10000, Status = "success" };
            var payload = new PaystackWebhookDto { Event = "charge.success", Data = data };

            // Compute valid signature using same algorithm to simulate Paystack
            using var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes(secret));
            var bytes = System.Text.Encoding.UTF8.GetBytes("{\"dummy\":true}");
            var hash = hmac.ComputeHash(bytes);
            var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            _mockPaymentService.Setup(p => p.ProcessSuccessfulWebhookAsync(It.IsAny<PaystackWebhookData>())).ReturnsAsync(true);

            var result = await _webhook.PaystackWebhookAsync(payload, "{\"dummy\":true}", signature);

            Assert.True(result);
            _mockPaymentService.Verify(p => p.ProcessSuccessfulWebhookAsync(It.IsAny<PaystackWebhookData>()), Times.Once);
        }
    }
}
