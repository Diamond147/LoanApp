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
        Task<ContinuationResponse<AdminUserDetailDto>> GetAllUsersDetailsAsync(int pageSize, string? continuationToken, string? userId, string? email, string? mobileNumber, string? gender, string? nationality, string? searchTerm);


        // User Management
        Task<ContinuationResponse<AdminUserDto>> GetUsersWithContinuationAsync(int pageSize, string? continuationToken, string? userId);
        Task<AdminUserDto?> GetUserByIdAsync(string userId);
        Task ChangeUserRoleAsync(string UserId, ChangeRoleDto dto);
        Task<bool> DeleteUserAsync(string userId);


        // Loan Management
        Task<ContinuationResponse<AdminLoanDto>> GetLoansWithContinuationAsync(int pageSize, string? continuationToken, LoanStatus? status = null, string? loanId = null);
        //Task<bool> MarkLoanAsPaidAsync(string loanId);
        Task<AdminLoanDto?> GetLoanByIdAsync(string loanId);
        Task<AdminLoanDto?> UpdateLoanStatusAsync(string loanId, LoanStatus newStatus);
        Task<bool> DeleteLoanAsync(string loanId);


        // PreQualified Management
        Task<PreQualifiedLoanDto?> CreatePreQualifiedLoanAsync(CreatePreQualifiedLoanDto createPqLoan);
        Task<List<PreQualifiedLoanDto>> GetAllPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId);
        Task<PreQualifiedLoanDto?> GetPreQualifiedLoanByIdAsync(string preQualifiedId);
        Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId);


        // History Management
        Task<bool> DeleteLoanHistoryAsync(string loanHistoryId);
    }
}
