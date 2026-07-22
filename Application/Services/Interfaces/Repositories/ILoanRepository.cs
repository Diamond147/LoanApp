using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Interfaces.Repositories
{
    public interface ILoanRepository
    {
        Task AddLoanAsync(Loan loan);

        //Task<(List<Loan> Loans, string? ContinuationToken)> GetAllLoansAsync(int pageSize, string? continuationToken, LoanStatus? status = null, string? loanId = null);
        //Task<Loan?> GetLoanByIdAsync(string loanId);

        Task<(List<Loan> Loans, string? ContinuationToken)> GetAllLoansAsync(int pageSize, string? continuationToken = null, LoanStatus? status = null, string? loanId = null);
        Task<List<Loan>> GetPaidLoansAsync(string userId);
        Task<bool> HasPaidLoanAsync(string userId);
        Task<bool> HasUnpaidLoanAsync(string userId);
        Task<Loan?> GetLoanByIdAsync(string loanId);
        Task<Loan?> GetApprovedLoanByUserIdAsync(string userId);
        Task<Loan?> UpdateLoanStatusAsync(string loanId, LoanStatus newStatus);
        Task UpdateLoanAsync(Loan loan);
        Task<bool> DeleteLoanAsync(string loanId);


        //Task AddLoanHistoryAsync(LoanHistory loanHistory);
        //Task<bool> historyExists(string loanId);


        //Task<List<PreQualifiedLoan>> GetAllPreQualifiedLoansAsync();
        //Task<PreQualifiedLoan?> GetPreQualifiedLoanByTypeAsync(LoanType loanType);
    }
}
