using Application.DTOs;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Payments;
using Domain.DTOs.Users.RequestDto;
using Domain.Enums;
// using Services.Interfaces; (interfaces moved to Application.Services.Interfaces.Services)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }



        [HttpGet("dashboardstats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return Ok(stats);
        }


        //[HttpGet("users/all-details")]
        //public async Task<IActionResult> GetAllUsersWithDetails(
        //    [FromQuery] int pageSize = 10,
        //    [FromQuery] string? continuationToken = null,
        //    [FromQuery] string? userId = null,
        //    [FromQuery] string? email = null,
        //    [FromQuery] string? mobileNumber = null,
        //    [FromQuery] string? gender = null,
        //    [FromQuery] string? nationality = null,
        //    [FromQuery] string? searchTerm = null)
        //{
        //    var result = await _adminService.GetAllUsersDetailsAsync(pageSize, continuationToken, userId, email, mobileNumber, gender, nationality, searchTerm);
        //    return Ok(result);
        //}

        ////[HttpGet("users")]
        ////public async Task<IActionResult> GetAllUsers(
        ////    [FromQuery] int pageSize = 10,
        ////    [FromQuery] string? continuationToken = null,
        ////    [FromQuery] string? userId = null)
        ////{
        ////    var result = await _adminService.GetUsersWithContinuationAsync(pageSize, continuationToken, userId);
        ////    return Ok(result);
        ////}

        //[HttpPost("users/{userId}/update-role")]
        //public async Task<IActionResult> ChangeRole(string userId, [FromBody] ChangeRoleDto dto)
        //{
        //    await _adminService.ChangeUserRoleAsync(userId, dto);
        //    return Ok(new { message = "User role successfully updated." });
        //}

        //[HttpDelete("users/{userId}")]
        //public async Task<IActionResult> DeleteUser(string userId)
        //{
        //    var result = await _adminService.DeleteUserAsync(userId);
        //    return Ok(new { message = "User deleted successfully" });
        //}



        //Loans
        //[HttpGet("loans")]
        //public async Task<IActionResult> GetAllLoans(
        //    [FromQuery] int pageSize = 10,
        //    [FromQuery] string? continuationToken = null,
        //    [FromQuery] LoanStatus? status = null,
        //    [FromQuery] string? loanId = null)
        //{
        //    var result = await _adminService.GetLoansWithContinuationAsync(pageSize, continuationToken, status, loanId);
        //    return Ok(result);
        //}

        //[HttpPost("loans/{loanId}/update-status")]
        //public async Task<IActionResult> UpdateLoanStatus(string loanId, [FromBody] LoanStatus newStatus)
        //{
        //    var result = await _adminService.UpdateLoanStatusAsync(loanId, newStatus);
        //    return Ok(new { message = $"Loan {newStatus} Successfully", data = result });
        //}

        ////[HttpPut("loans{loanId}/mark-as-paid")]
        ////public async Task<IActionResult> MarkLoanAsPaid(string loanId)
        ////{
        ////        var result = await _adminService.MarkLoanAsPaidAsync(loanId);
        ////        return Ok(new { message = "Loan marked as paid successfully" });
        ////}

        //[HttpDelete("loans/{loanId}")]
        //public async Task<IActionResult> DeleteLoan(string loanId)
        //{
        //    var result = await _adminService.DeleteLoanAsync(loanId);
        //    return Ok(new { message = "Loan deleted successfully" });
        //}


        //[HttpDelete("histories/{loanHistoryId}")]
        //public async Task<IActionResult> DeleteLoanHistory(string loanHistoryId)
        //{
        //    var result = await _adminService.DeleteLoanHistoryAsync(loanHistoryId);
        //    return Ok(new { message = "Loan history deleted successfully" });
        //}
    }
}
