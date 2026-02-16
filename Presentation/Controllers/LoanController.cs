using Application.Services.Interfaces;
using Domain.DTOs.Payments;
using Domain.DTOs.Users.RequestDto;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class LoanController : ControllerBase
    {
        private readonly ILoanService _loanService;
        private readonly IDashboardService _dashboardService;
        private readonly IUserService _userService;
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;


        public LoanController(ILoanService loanService, IDashboardService dashboardService, IUserService userService, IPaymentService paymentService, IEmailService emailService)
        {
            _loanService = loanService;
            _dashboardService = dashboardService;
            _userService = userService;
            _paymentService=paymentService;
            _emailService=emailService;
        }

        [HttpGet("userprofiles")]
        public async Task<IActionResult> GetOrCreateUserProfile()
        {
            var userProfile = await _userService.GetOrCreateUserProfileAsync();
            return Ok(userProfile);
        }

        [HttpPut("userprofiles/complete")]
        public async Task<IActionResult> CompleteUserProfile([FromBody] CompleteProfileDto dto)
        {
            var profile = await _userService.CompleteUserProfileAsync(dto);
            return Ok(profile);
        }


        //[HttpPut("users/update")]
        //public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto dto)
        //{
        //        var profile = await _userService.UpdateUserAsync(dto);
        //        return Ok(profile);
        //}

        [HttpPatch("users/{userId}")]
        public async Task<IActionResult> PatchUser(string userId, [FromBody] PatchUserProfileDto PatchUser)
        {
            var result = await _userService.PatchUserAsync(userId, PatchUser);
            return Ok(result);
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var success = await _userService.DeleteUserAsync(userId);
            return NoContent();
        }


        [HttpGet("allprequalifiedloans")]
        public async Task<IActionResult> GetAllPrequalifiedLoans()
        {
            var loans = await _loanService.GetAllPreQualifiedLoansAsync();
            return Ok(loans);
        }

        [HttpPost("loans")]
        public async Task<IActionResult> CreateLoan([FromQuery] LoanType loantype, [FromBody] CreateLoanDto createLoan)
        {
            var loan = await _loanService.CreateLoanAsync(loantype, createLoan);
            return CreatedAtAction(nameof(CreateLoan), new { id = loan?.Id }, loan);
        }

        [HttpGet("loans")]
        public async Task<IActionResult> GetLoans(
            [FromQuery] int pageSize = 10,
            [FromQuery] string? continuationToken = null,
            [FromQuery] LoanStatus? status = null,
            [FromQuery] string? loanId = null)
        {
            var result = await _loanService.GetLoansWithContinuationAsync(pageSize, continuationToken, status, loanId);
            return Ok(result);
        }



        //PAYMENTS

        [HttpPost("loanpayment")]
        [AllowAnonymous]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentDto initiatePayment)
        {
            var result = await _paymentService.InitiatePaymentAsync(initiatePayment);
            return Ok(new
            {
                success = true,
                message = "Payment initialized successfully. Please complete payment at the provided URL.",
                data = result
            });
        }

        [HttpGet("payments")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPayments([FromQuery] PaymentStatus? status = null, [FromQuery] string? paymentId = null, [FromQuery] string? reference = null)
        {
            var result = await _paymentService.GetPaymentsAsync(status, paymentId, reference);
            return Ok(result);
        }

    }
}
