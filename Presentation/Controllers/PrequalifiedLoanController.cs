using Application.Services.Implementations;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/prequalifiedloans")]
    [Authorize(Roles = "Admin")]
    public class PrequalifiedLoanController : ControllerBase
    {
        private readonly IPrequalifiedLoanService _prequalifiedLoanService;

        public PrequalifiedLoanController(IPrequalifiedLoanService prequalifiedLoanService)
        {
            _prequalifiedLoanService = prequalifiedLoanService;
        }


        [HttpPost]
        public async Task<IActionResult> CreatePreQualifiedLoan([FromBody] CreatePreQualifiedLoanDto createPqLoan)
        {
            var preQualifiedLoan = await _prequalifiedLoanService.CreatePreQualifiedLoanAsync(createPqLoan);
            return CreatedAtAction(nameof(CreatePreQualifiedLoan), new { id = preQualifiedLoan?.Id }, preQualifiedLoan);
        }


        
        [Authorize(Roles = "User, Admin")]      // For users to know the available prequalifiedloans before creating a loan
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPrequalifiedLoans()
        {
            var result = await _prequalifiedLoanService.GetAllPreQualifiedLoansAsync();
            return Ok(result);
        }


        [HttpGet]
        public async Task<IActionResult> GetPreQualifiedLoans(
            [FromQuery] string? preQualifiedId = null,
            [FromQuery] LoanType? loanType = null)
        {
            var result = await _prequalifiedLoanService.GetPreQualifiedLoansAsync(loanType, preQualifiedId);
            return Ok(result);
        }


        [HttpDelete("{preQualifiedId}")]
        public async Task<IActionResult> DeletePreQualifiedLoan(string preQualifiedId)
        {
            var result = await _prequalifiedLoanService.DeletePreQualifiedLoanAsync(preQualifiedId);
            return Ok(new { message = "PreQualified loan deleted successfully" });
        }
    }
}
