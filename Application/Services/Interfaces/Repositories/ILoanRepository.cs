using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Interfaces.Repositories
{
    public interface ILoanRepository
    {
        Task AddLoanAsync(Loan loan);
        Task<(List<Loan> Loans, string? ContinuationToken)> GetLoansWithContinuationAsync(int pageSize, string? continuationToken = null, LoanStatus? status = null, string? loanId = null);
        Task<bool> HasUnpaidLoanAsync(string userId);
        Task<Loan?> GetLoanByIdAsync(string loanId);
        Task<Loan?> GetApprovedLoanByUserIdAsync(string userId);
        Task UpdateLoanAsync(Loan loan);
        Task AddLoanHistoryAsync(LoanHistory loanHistory);
        Task<bool> historyExists(string loanId);

        Task<List<PreQualifiedLoan>> GetAllPreQualifiedLoansAsync();
        Task<PreQualifiedLoan?> GetPreQualifiedLoanByTypeAsync(LoanType loanType);
    }
}
