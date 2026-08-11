using Application.Services.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Infrastructure.Repositories.Implementations
{
    [ApiController]
    [Route("api/v1/loans")]
    [Authorize]
    public class LoanHistoryController : ControllerBase
    {
        private readonly ILoanHistoryService _loanHistoryService;

        public LoanHistoryController(ILoanHistoryService loanHistoryService)
        {
            _loanHistoryService = loanHistoryService;
        }



        [Authorize(Roles = "User,Admin")]
        [HttpGet("{loanId}/history")]
        public async Task<IActionResult> GetLoanHistoryByLoanId(string loanId)
        {
            var histories = await _loanHistoryService.GetLoanHistoryByLoanIdAsync(loanId);
            return Ok(histories);
        }


        [Authorize(Roles = "User,Admin")]
        [HttpGet("history/{historyId}")]
        public async Task<IActionResult> GetLoanHistoryByHistoryId(string historyId)
        {
            var history = await _loanHistoryService.GetLoanHistoryByHistoryIdAsync(historyId);

            if (history == null)
                return NotFound(new { message = $"Loan history record with ID '{historyId}' was not found." });

            return Ok(history);
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete("history/{loanHistoryId}")]
        public async Task<IActionResult> DeleteLoanHistory(string loanHistoryId)
        {
            var result = await _loanHistoryService.DeleteLoanHistoryAsync(loanHistoryId);
            return Ok(new { message = "Loan history deleted successfully" });
        }

    }
}
