using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Interfaces.Repositories
{
    public interface IAdminRepository
    {

        // Dashboard Statistics
        Task<AdminDashboardStats> GetDashboardStatsAsync();
        Task<(List<UserProfile> Users, string? ContinuationToken)> GetAllUsersDetailsAsync(
            int pageSize = 10,
            string? continuationToken = null,
            string? userId = null,
            string? email = null,
            string? mobileNumber = null,
            string? gender = null,
            string? nationality = null,
            string? searchTerm = null);


        // User Management
        Task<(List<UserProfile> UserProfiles, string? ContinuationToken)> GetUsersWithContinuationAsync(int pageSize, string? continuationToken, string? userId = null);
        Task<UserProfile?> GetUserByIdAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);


        // Loan Management
        Task<(List<Loan> Loans, string? ContinuationToken)> GetLoansWithContinuationAsync(int pageSize, string? continuationToken, LoanStatus? status = null, string? loanId = null);
        Task<bool> HasPaidLoanAsync(string userId);
        Task<List<Loan>> GetPaidLoansAsync(string userId);
        Task<Loan?> GetLoanByIdAsync(string loanId);
        Task<Loan?> UpdateLoanStatusAsync(string loanId, LoanStatus newStatus);
        Task<bool> DeleteLoanAsync(string loanId);


        // PreQualified Management
        Task AddPreQualifiedLoanAsync(PreQualifiedLoan preQualifiedLoan);
        Task<List<PreQualifiedLoan>> GetAllPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId = null);
        Task<PreQualifiedLoan?> GetPreQualifiedLoanByIdAsync(string preQualifiedId);
        Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId);


        // History Management
        Task AddLoanHistoryAsync(LoanHistory loanHistory);
        Task<bool> DeleteLoanHistoryAsync(string loanHistoryId);

    }
}
