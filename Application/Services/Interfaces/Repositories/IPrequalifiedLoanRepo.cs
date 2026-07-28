using Domain.Entities;
using Domain.Enums;


namespace Application.Services.Interfaces.Repositories
{
    public interface IPrequalifiedLoanRepo
    {
        Task AddPreQualifiedLoanAsync(PreQualifiedLoan preQualifiedLoan);
        Task<List<PreQualifiedLoan>> GetPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId = null);
        Task<List<PreQualifiedLoan>> GetAllPreQualifiedLoansAsync();
        Task<PreQualifiedLoan?> GetPreQualifiedLoanByIdAsync(string preQualifiedId);
        Task<PreQualifiedLoan?> GetPreQualifiedLoanByTypeAsync(LoanType? loanType);
        Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId);
    }
}
