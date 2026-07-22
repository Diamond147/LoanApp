using Domain.DTOs.Users.ResponseDto;

namespace Application.Services.Interfaces.Services
{
    public interface ILoanHistoryService
    {
        Task<IEnumerable<LoanHistoryDto>> GetLoanHistoryByLoanIdAsync(string loanId);
        Task<LoanHistoryDto?> GetLoanHistoryByHistoryIdAsync(string historyId);
        Task<bool> DeleteLoanHistoryAsync(string loanHistoryId);
    }
}
