using Application.Services.Interfaces.Services;
using Domain.DTOs.Payments;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/payments")]
    [Authorize(Roles = "User,Admin")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaystackWebhook _paystackWebhook;


        public PaymentController(IPaymentService paymentService, IPaystackWebhook paystackWebhook)
        {
            _paymentService = paymentService;
            _paystackWebhook = paystackWebhook;
        }


 
        [HttpPost]
        public async Task<IActionResult> InitiatePayment()
        {
            var result = await _paymentService.InitiatePaymentAsync();
            return Ok(new
            {
                success = true,
                message = "Payment initialized successfully. Please complete payment at the provided URL.",
                data = result
            });
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetPayments([FromQuery] PaymentStatus? status = null, [FromQuery] string? paymentId = null, [FromQuery] string? reference = null)
        {
            var result = await _paymentService.GetPaymentsAsync(status, paymentId, reference);
            return Ok(result);
        }


        [AllowAnonymous]
        [HttpGet("reference")]
        public async Task<IActionResult> GetByReference([FromQuery] string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return BadRequest(new { message = "Payment reference is required." });
            }

            var result = await _paymentService.GetPaymentByReferenceAsync(reference);

            if (result == null)
            {
                return NotFound(new { statusCode = 404, message = "Payment not found." });
            }

            return Ok(result);
        }


        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaystackWebhook([FromBody] PaystackWebhookDto payload)
        {
            // Extract raw body for signature verification
            Request.Body.Position = 0;
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();

            // Reset stream position again so MVC model binder or down-stream middleware doesn't read empty body
            Request.Body.Position = 0;

            var signature = Request.Headers["x-paystack-signature"].ToString();

            // Call External Service
            var isValid = await _paystackWebhook.PaystackWebhookAsync(payload, requestBody, signature);

            // If signature fails, return 400; otherwise 200
            return isValid ? Ok(new { message = "Webhook received" }) : BadRequest(new { message = "Invalid signature" });
        }
    }
}
