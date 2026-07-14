using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Enums;

namespace Application.Services.Interfaces.Services
{
    public interface ILoanService
    {
        Task<List<PreQualifiedLoanDto>> GetAllPreQualifiedLoansAsync();
        Task<LoanDto?> CreateLoanAsync(CreateLoanDto createLoan);
        Task<ContinuationResponse<LoanDto>> GetLoansWithContinuationAsync(int pageSize, string? continuationToken, LoanStatus? status, string? loanId);
        Task<LoanDto?> GetLoanByIdAsync(string loanId);
    }
}
