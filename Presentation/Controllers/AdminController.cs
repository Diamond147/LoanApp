using Application.Services.Interfaces;
using Domain.DTOs.Payments;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;
        private readonly IDashboardService _dashboardService;

        public AdminController(IAdminService adminService, IPaymentService paymentService, IConfiguration configuration, IDashboardService dashboardService)
        {
            _adminService = adminService;
            _paymentService=paymentService;
            _configuration=configuration;
            _dashboardService=dashboardService;
        }

        [HttpGet("dashboardstats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return Ok(stats);
        }

        [HttpGet("filter-users")]
        public async Task<IActionResult> FilterUsersWithDetails(
            [FromQuery] string? userId, 
            [FromQuery] string? email, 
            [FromQuery] string? mobileNumber, 
            [FromQuery] string? search, 
            [FromQuery] string? gender, 
            [FromQuery] string? nationality)
        {
            LoanDashboardDto dashboard;

            if (!string.IsNullOrEmpty(userId))
                dashboard = await _dashboardService.GetDashboardByIdAsync(userId);
            else if (!string.IsNullOrEmpty(email))
                dashboard = await _dashboardService.GetDashboardByEmailAsync(email);
            else if (!string.IsNullOrEmpty(mobileNumber))
                dashboard = await _dashboardService.GetDashboardByMobileAsync(mobileNumber);
            else if (!string.IsNullOrEmpty(search))
                dashboard = await _dashboardService.SearchDashboardAsync(search);

            else if (!string.IsNullOrEmpty(gender))
            {
                var dashboards = await _dashboardService.GetDashboardsByGenderAsync(gender);
                return Ok(dashboards);
            }
            else if (!string.IsNullOrEmpty(nationality))
            {
                var dashboards = await _dashboardService.GetDashboardsByNationalityAsync(nationality);
                return Ok(dashboards);
            }

            else
                return BadRequest(new { message = "provide at least one query parameter: userId, email, mobileNumber, search, gender, nationality" });

            return Ok(dashboard);
        }


        [HttpGet("users/all-details")]
        public async Task<IActionResult> GetAllUsersWithDetails(
            [FromQuery] int pageSize = 10,
            [FromQuery] string? continuationToken = null,
            [FromQuery] string? userId = null)
        {
            var result = await _adminService.GetAllUsersDetailsAsync(pageSize, continuationToken, userId);
            return Ok(result);
        }

        //[HttpGet("users")]
        //public async Task<IActionResult> GetAllUsers(
        //    [FromQuery] int pageSize = 10,
        //    [FromQuery] string? continuationToken = null,
        //    [FromQuery] string? userId = null)
        //{
        //    var result = await _adminService.GetUsersWithContinuationAsync(pageSize, continuationToken, userId);
        //    return Ok(result);
        //}

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var result = await _adminService.DeleteUserAsync(userId);
            return Ok(new { message = "User deleted successfully" });
        }



        [HttpGet("loans")]
        public async Task<IActionResult> GetAllLoans(
            [FromQuery] int pageSize = 10,
            [FromQuery] string? continuationToken = null,
            [FromQuery] LoanStatus? status = null,
            [FromQuery] string? loanId = null)
        {
            var result = await _adminService.GetLoansWithContinuationAsync(pageSize, continuationToken, status, loanId);
            return Ok(result);
        }

        [HttpPost("loans/updatestatus")]
        public async Task<IActionResult> UpdateLoanStatus([FromQuery] string loanId, [FromQuery] LoanStatus newStatus)
        {
            var result = await _adminService.UpdateLoanStatusAsync(loanId, newStatus);
            return Ok(new { message = $"Loan {newStatus} Successfully", data = result });
        }

        //[HttpPut("loans{loanId}/mark-as-paid")]
        //public async Task<IActionResult> MarkLoanAsPaid(string loanId)
        //{
        //        var result = await _adminService.MarkLoanAsPaidAsync(loanId);
        //        return Ok(new { message = "Loan marked as paid successfully" });
        //}

        [HttpDelete("loans/{loanId}")]
        public async Task<IActionResult> DeleteLoan(string loanId)
        {
            var result = await _adminService.DeleteLoanAsync(loanId);
            return Ok(new { message = "Loan deleted successfully" });
        }



        [HttpPost("prequalifiedloans")]
        public async Task<IActionResult> CreatePreQualifiedLoan([FromQuery] LoanType loanType, [FromBody] CreatePreQualifiedLoanDto createPqLoan)
        {
            var preQualifiedLoan = await _adminService.CreatePreQualifiedLoanAsync(loanType, createPqLoan);
            return CreatedAtAction(nameof(CreatePreQualifiedLoan), new { id = preQualifiedLoan?.Id }, preQualifiedLoan);
        }

        [HttpGet("prequalifiedloans")]
        public async Task<IActionResult> GetPreQualifiedLoans(
            [FromQuery] string? preQualifiedId = null,
            [FromQuery] LoanType? loanType = null)
        {
            var allPreQualified = await _adminService.GetAllPreQualifiedLoansAsync(loanType, preQualifiedId);
            return Ok(allPreQualified);
        }

        [HttpDelete("prequalifiedloans/{preQualifiedId}")]
        public async Task<IActionResult> DeletePreQualifiedLoan(string preQualifiedId)
        {
            var result = await _adminService.DeletePreQualifiedLoanAsync(preQualifiedId);
            return Ok(new { message = "PreQualified loan deleted successfully" });
        }



        [HttpDelete("histories/{loanHistoryId}")]
        public async Task<IActionResult> DeleteLoanHistory(string loanHistoryId)
        {
            var result = await _adminService.DeleteLoanHistoryAsync(loanHistoryId);
            return Ok(new { message = "Loan history deleted successfully" });
        }



        // WEBHOOK
        [HttpPost("paystack")]
        [AllowAnonymous]
        public async Task<IActionResult> PaystackWebhook([FromBody] PaystackWebhookDto payload)
        {
            Request.EnableBuffering(); // Allows reading body multiple times

            Request.Body.Position = 0;   // 2. RESET POSITION POINTER TO THE BEGINNING (Crucial!)

            using var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();

            Request.Body.Position = 0; // Reset stream position again

            Console.WriteLine($"Webhook received at {DateTime.UtcNow}");
            Console.WriteLine($"Request body length: {requestBody.Length}");
            Console.WriteLine($"Request body: {requestBody}");

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                Console.WriteLine("EMPTY BODY RECEIVED - Check if Paystack sent data.");
                return Ok(); // Still return 200 to stop retries
            }

            // Get webhook secret from configuration
            var secret = _configuration["paystack: Webhook"];

            // Skip signature check if secret not configured
            if (!string.IsNullOrEmpty(secret))
            {
                // Get signature from Request header
                var signature = Request.Headers["x-paystack-signature"].ToString();

                if (!VerifySignature(requestBody, signature, secret))
                {
                    return BadRequest(new { message = "Invalid signature" });
                }
            }

            // Parse and process webhook data
            try
            {
                if (payload.Event == "charge.success")
                {
                    var reference = payload.Data.Reference;

                    Console.WriteLine($"Processing payment for reference: {reference}");

                    var result = await _paymentService.VerifyPaymentAsync(reference);
                    if (result)
                    {
                        Console.WriteLine("Payment processed successfully");
                    }
                    else
                    {
                        Console.WriteLine("Failed to process payment");
                    }
                }
                else
                {
                    // Other event type - we're not interested
                    Console.WriteLine($"Ignoring event type: {payload.Event}");
                }

                return Ok(new { message = "Webhook received" });
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                return Ok(); // Return 200 so Paystack doesn't keep hitting your error
            }
        }

        private bool VerifySignature(string payload, string signature, string secret)
        {
            if (string.IsNullOrEmpty(secret)) return true; // Skip if no secret

            // NOTE: HMAC algorithm works with byte arrays
            var secretBytes = Encoding.UTF8.GetBytes(secret);

            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            // Create HMAC SHA-512 hasher with secret key
            using var hmac = new HMACSHA512(secretBytes);

            // This creates a byte array containing the hash
            var hashBytes = hmac.ComputeHash(payloadBytes);

            // Convert hash bytes to hexadecimal string bcos Paystack sends signature as hex string
            var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            // Compare computed signature with signature from Paystack
            return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }
    }
}
