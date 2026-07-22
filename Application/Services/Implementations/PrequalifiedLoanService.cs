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
            try
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
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        public async Task<List<PreQualifiedLoanDto>> GetPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId)
        {
            try
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
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<List<PreQualifiedLoanDto>> GetAllPreQualifiedLoansAsync()
        {
            try
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
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<PreQualifiedLoanDto?> GetPreQualifiedLoanByIdAsync(string preQualifiedId)
        {
            try
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
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId)
        {
            try
            {
                var deleted = await _prequalifiedLoanRepo.DeletePreQualifiedLoanAsync(preQualifiedId);
                if (!deleted)
                    throw new NotFoundException("PreQualifiedLoan not found");

                return deleted;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }
    }
}
