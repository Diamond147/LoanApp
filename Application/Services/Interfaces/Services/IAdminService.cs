using Application.DTOs;
using Domain.DTOs.Admin;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Interfaces.Services
{
    public interface IAdminService
    {
        // Dashboard
        Task<AdminDashboardStats> GetDashboardStatsAsync();


        // User Management
        //Task<ContinuationResponse<AdminUserDetailDto>> GetAllUsersDetailsAsync(int pageSize, string? continuationToken, string? userId, string? email, string? mobileNumber, string? gender, string? nationality, string? searchTerm);
        //Task<ContinuationResponse<AdminUserDto>> GetAllUsersAsync(int pageSize, string? continuationToken, string? userId);
        //Task<AdminUserDto?> GetUserByIdAsync(string userId);
        //Task ChangeUserRoleAsync(string UserId, ChangeRoleDto dto);
        //Task<bool> DeleteUserAsync(string userId);


        // Loan Management
        //Task<ContinuationResponse<AdminLoanDto>> GetAllLoansAsync(int pageSize, string? continuationToken, LoanStatus? status = null, string? loanId = null);
        ////Task<bool> MarkLoanAsPaidAsync(string loanId);
        //Task<AdminLoanDto?> GetLoanByIdAsync(string loanId);
        //Task<AdminLoanDto?> UpdateLoanStatusAsync(string loanId, LoanStatus newStatus);
        //Task<bool> DeleteLoanAsync(string loanId);


        // History Management
        //Task<bool> DeleteLoanHistoryAsync(string loanHistoryId);
    }
}
