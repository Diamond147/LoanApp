using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Enums;

namespace Application.Services.Interfaces
{
    public interface ILoanService
    {
        Task<List<PreQualifiedLoanDto>> GetAllPreQualifiedLoansAsync();

        Task<LoanDto?> CreateLoanAsync(LoanType loanType, CreateLoanDto createLoan);
        Task<ContinuationResponse<LoanDto>> GetLoansWithContinuationAsync(int pageSize, string? continuationToken, LoanStatus? status, string? loanId);
        Task<LoanDto?> GetLoanByIdAsync(string loanId);
    }
}
