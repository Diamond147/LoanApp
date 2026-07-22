using Domain.Entities;


namespace Application.Services.Interfaces.Repositories
{
    public interface ILoanHistoryRepository
    {
        Task AddLoanHistoryAsync(LoanHistory loanHistory);
        Task<IEnumerable<LoanHistory>> GetLoanHistoryByLoanIdAsync(string loanId);
        Task<LoanHistory?> GetLoanHistoryByHistoryIdAsync(string historyId);
        Task<bool> historyExists(string loanId);
        Task<bool> DeleteLoanHistoryAsync(string loanHistoryId);
    }
}
