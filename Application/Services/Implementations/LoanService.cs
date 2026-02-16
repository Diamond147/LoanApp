using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace Application.Services.Implementations
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private  readonly IHttpContextAccessor _httpContextAccessor;

        public LoanService(ILoanRepository loanRepository, IHttpContextAccessor httpContextAccessor)
        {
            _loanRepository = loanRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<PreQualifiedLoanDto>> GetAllPreQualifiedLoansAsync()
        {
            try
            {
                var preQualifiedLoans = await _loanRepository.GetAllPreQualifiedLoansAsync();
                return preQualifiedLoans.Select(p => new PreQualifiedLoanDto
                {
                    LoanType = p.LoanType,
                    MinAmount = p.MinAmount,
                    MaxAmount = p.MaxAmount,
                    LoanTenure = p.LoanTenure,
                }).ToList();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (CosmosException ex) when (
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        public async Task<LoanDto?> CreateLoanAsync(LoanType loanType, CreateLoanDto createLoan)
        {
            try
            {

                var UserInfo = _httpContextAccessor.HttpContext?.User?.GetUserInfo(); // Get all user info at once
                if (UserInfo == null)
                    throw new UnauthorizedAccessException("User is not authenticated");

                var AuthUserId = UserInfo.UserId;

                var hasUnpaidLoan = await _loanRepository.HasUnpaidLoanAsync(AuthUserId);
                if (hasUnpaidLoan)
                    throw new ValidationException("You have an existing unpaid loan that must be settled first");

                var existingLoanType = await _loanRepository.GetPreQualifiedLoanByTypeAsync(loanType);
                if (existingLoanType == null)
                    throw new NotFoundException($"Loan of type {loanType} is not currently available");

                if (createLoan.Amount <= 0)
                    throw new ValidationException("Requested amount must be greater than zero");

                if (createLoan.Amount < existingLoanType.MinAmount || createLoan.Amount > existingLoanType.MaxAmount)
                    throw new ValidationException($"Incorrect amount for the {loanType} loan type.");

                var loan = new Loan
                {
                    UserProfileId = AuthUserId,
                    LoanType = loanType,
                    Amount = createLoan.Amount,
                    RequestedDate = DateTime.UtcNow,
                    Status = LoanStatus.Pending,
                };
                await _loanRepository.AddLoanAsync(loan);

                //History record
                var loanHistory = new LoanHistory
                {
                    LoanId = loan.Id,
                    LoanType = loan.LoanType,
                    RequestedAmount = loan.Amount,
                    RequestedDate = DateTime.UtcNow,
                    Status = loan.Status,
                    UserProfileId = AuthUserId,
                };
                await _loanRepository.AddLoanHistoryAsync(loanHistory);

                return new LoanDto
                {
                    Id = loan.Id,
                    LoanType = loan.LoanType,
                    RequestedAmount = loan.Amount,
                    RequestedDate = loan.RequestedDate,
                    Status = loan.Status,
                    UserProfileId = loan.UserProfileId
                };
            }
            catch (CosmosException ex) when (
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<ContinuationResponse<LoanDto>> GetLoansWithContinuationAsync(int pageSize, string? continuationToken, LoanStatus? status, string? loanId)
        {
            try
            {
                if (pageSize < 1 || pageSize > 100)
                {
                    throw new NotFoundException("PageSize must be between 1 and 100" );
                }
                var (loans, newContinuationToken) = await _loanRepository.GetLoansWithContinuationAsync(pageSize, continuationToken, status, loanId);

                var loanDtos = new List<LoanDto>();

                foreach (var loan in loans)
                {
                    loanDtos.Add(new LoanDto
                    {
                        Id = loan.Id,
                        LoanType = loan.LoanType,
                        RequestedAmount = loan.Amount,
                        Status = loan.Status,
                        RequestedDate = loan.RequestedDate,
                        UserProfileId = loan.UserProfileId,
                    });
                }
                return new ContinuationResponse<LoanDto>
                {
                    Data = loanDtos,
                    ContinuationToken = newContinuationToken,
                    HasMore = !string.IsNullOrEmpty(newContinuationToken)
                };
            }
            catch (CosmosException ex) when (
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<LoanDto?> GetLoanByIdAsync(string loanId)
        {
            try
            {
                var loan = await _loanRepository.GetLoanByIdAsync(loanId);
                if (loan == null)
                    throw new NotFoundException("Loan not found");
                return new LoanDto
                {
                    Id = loan.Id,
                    LoanType = loan.LoanType,
                    RequestedAmount = loan.Amount,
                    RequestedDate = loan.RequestedDate,
                    Status = loan.Status,
                    UserProfileId = loan.UserProfileId
                };
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (CosmosException ex) when (
               ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

    }
}
