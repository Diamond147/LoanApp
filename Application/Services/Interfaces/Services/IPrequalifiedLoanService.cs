using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces.Services
{
    public interface IPrequalifiedLoanService
    {
        Task<PreQualifiedLoanDto?> CreatePreQualifiedLoanAsync(CreatePreQualifiedLoanDto createPqLoan);
        Task<List<PreQualifiedLoanDto>> GetPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId);
        Task<List<PreQualifiedLoanDto>> GetAllPreQualifiedLoansAsync();
        Task<PreQualifiedLoanDto?> GetPreQualifiedLoanByIdAsync(string preQualifiedId);
        Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId);
    }
}
