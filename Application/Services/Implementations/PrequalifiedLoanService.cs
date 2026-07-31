using Application.Exceptions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Implementations
{
    public class PrequalifiedLoanService : IPrequalifiedLoanService
    {
        private readonly IPrequalifiedLoanRepo _prequalifiedLoanRepo;

        public PrequalifiedLoanService(IPrequalifiedLoanRepo prequalifiedLoanRepo)
        {
            _prequalifiedLoanRepo = prequalifiedLoanRepo;
        }


        public async Task<PreQualifiedLoanDto?> CreatePreQualifiedLoanAsync(CreatePreQualifiedLoanDto createPqLoan)
        {
            var preQualifiedLoan = new PreQualifiedLoan
            {
                LoanType = createPqLoan.LoanType,
                MinAmount = createPqLoan.MinAmount,
                MaxAmount = createPqLoan.MaxAmount,
                LoanTenureInMonths = createPqLoan.LoanTenureInMonths,
                CreatedAt = DateTime.UtcNow
            };

            await _prequalifiedLoanRepo.AddPreQualifiedLoanAsync(preQualifiedLoan);

            return new PreQualifiedLoanDto
            {
                Id = preQualifiedLoan.Id,
                LoanType = preQualifiedLoan.LoanType,
                MinAmount = preQualifiedLoan.MinAmount,
                MaxAmount = preQualifiedLoan.MaxAmount,
                LoanTenureInMonths = preQualifiedLoan.LoanTenureInMonths,
                CreatedAt = preQualifiedLoan.CreatedAt
            };
        }


        public async Task<List<PreQualifiedLoanDto>> GetPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId)
        {
            var allPreQualified = await _prequalifiedLoanRepo.GetPreQualifiedLoansAsync(loanType, preQualifiedId);
            if (allPreQualified == null)
            {
                return new List<PreQualifiedLoanDto>();
            }

            return allPreQualified.Select(p => new PreQualifiedLoanDto
            {
                Id = p.Id,
                LoanType = p.LoanType,
                MaxAmount = p.MaxAmount,
                MinAmount = p.MinAmount,
                LoanTenureInMonths = p.LoanTenureInMonths,
                CreatedAt = p.CreatedAt
            }).ToList();
        }


        public async Task<List<PreQualifiedLoanDto>> GetAllPreQualifiedLoansAsync()
        {
            var preQualifiedLoans = await _prequalifiedLoanRepo.GetAllPreQualifiedLoansAsync();
            return preQualifiedLoans.Select(p => new PreQualifiedLoanDto
            {
                LoanType = p.LoanType,
                MinAmount = p.MinAmount,
                MaxAmount = p.MaxAmount,
                LoanTenureInMonths = p.LoanTenureInMonths,
                CreatedAt = p.CreatedAt
            }).ToList();
        }


        public async Task<PreQualifiedLoanDto?> GetPreQualifiedLoanByIdAsync(string preQualifiedId)
        {
            var preQualified = await _prequalifiedLoanRepo.GetPreQualifiedLoanByIdAsync(preQualifiedId);
            if (preQualified == null)
                throw new NotFoundException("PreQualified not found");

            return new PreQualifiedLoanDto
            {
                Id = preQualified.Id,
                LoanType = preQualified.LoanType,
                MinAmount = preQualified.MinAmount,
                MaxAmount = preQualified.MaxAmount,
                LoanTenureInMonths = preQualified.LoanTenureInMonths,
                CreatedAt = preQualified.CreatedAt
            };
        }


        public async Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId)
        {
            var deleted = await _prequalifiedLoanRepo.DeletePreQualifiedLoanAsync(preQualifiedId);
            if (!deleted)
                throw new NotFoundException("PreQualifiedLoan not found");

            return deleted;
        }
    }
}
