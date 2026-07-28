using Domain.DTOs.Admin;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Enums;

namespace Application.Services.Interfaces.Services
{
    public interface ILoanService
    {
        Task<LoanDto?> CreateLoanAsync(CreateLoanDto createLoan);
        Task<ContinuationResponse<LoanDto>> GetAllLoansAsync(int pageSize, string? continuationToken, LoanStatus? status, string? loanId);
        Task<LoanDto?> GetLoanByIdAsync(string loanId, string userId);
        Task<LoanDto?> UpdateLoanStatusAsync(string loanId, LoanStatus newStatus);
        Task<bool> DeleteLoanAsync(string loanId);

        //Task<ContinuationResponse<AdminLoanDto>> GetAllLoansAsync(int pageSize, string? continuationToken, LoanStatus? status = null, string? loanId = null);
        //Task<bool> MarkLoanAsPaidAsync(string loanId);
        //Task<AdminLoanDto?> GetLoanByIdAsync(string loanId);
    }
}
