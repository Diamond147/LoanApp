using Application.Services.Implementations;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/loans")]
    [Authorize(Roles = "User,Admin")]
    public class LoanController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoanController(ILoanService loanService)
        {
            _loanService = loanService;
        }


        //// need to change the routing of this enndpoint...
        //[HttpGet("prequalifiedloans")]
        //public async Task<IActionResult> GetAllPrequalifiedLoans()
        //{
        //    var loans = await _loanService.GetAllPreQualifiedLoansAsync();
        //    return Ok(loans);
        //}


        [HttpPost]
        public async Task<IActionResult> CreateLoan([FromBody] CreateLoanDto createLoan)
        {
            var loan = await _loanService.CreateLoanAsync(createLoan);
            return CreatedAtAction(nameof(CreateLoan), new { id = loan?.Id }, loan);
        }


        [HttpGet("{loanId}")]
        public async Task<IActionResult> GetLoanById(string loanId)
        {
            // Extract the logged-in user's ID from JWT claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var loan = await _loanService.GetLoanByIdAsync(loanId, userId);
            if (loan == null)
                return NotFound();

            return Ok(loan);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetLoans(
            [FromQuery] int pageSize = 10,
            [FromQuery] string? continuationToken = null,
            [FromQuery] LoanStatus? status = null,
            [FromQuery] string? loanId = null)
        {
            var result = await _loanService.GetAllLoansAsync(pageSize, continuationToken, status, loanId);
            return Ok(result);
        }



        [Authorize(Roles = "Admin")]
        [HttpPost("{loanId}/status")]
        public async Task<IActionResult> UpdateLoanStatus(string loanId, [FromBody] LoanStatus newStatus)
        {
            var result = await _loanService.UpdateLoanStatusAsync(loanId, newStatus);
            return Ok(new { message = $"Loan {newStatus} Successfully", data = result });
        }


        //[HttpPut("loans{loanId}/mark-as-paid")]
        //public async Task<IActionResult> MarkLoanAsPaid(string loanId)
        //{
        //        var result = await _adminService.MarkLoanAsPaidAsync(loanId);
        //        return Ok(new { message = "Loan marked as paid successfully" });
        //}


        [Authorize(Roles = "Admin")]
        [HttpDelete("{loanId}")]
        public async Task<IActionResult> DeleteLoan(string loanId)
        {
            var result = await _loanService.DeleteLoanAsync(loanId);
            return Ok(new { message = "Loan deleted successfully" });
        }
    }
}
