using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Admin;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Application.Services.Implementations
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private  readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ILoanHistoryRepository _loanHistoryRepository;
        private readonly IPrequalifiedLoanRepo _prequalifiedLoanRepo;


        public LoanService(ILoanRepository loanRepository, IHttpContextAccessor httpContextAccessor, IUserRepository userRepository, IEmailService emailService, ILoanHistoryRepository loanHistoryRepository, IPrequalifiedLoanRepo prequalifiedLoanRepo)
        {
            _loanRepository = loanRepository;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _emailService = emailService;
            _loanHistoryRepository = loanHistoryRepository;
            _prequalifiedLoanRepo = prequalifiedLoanRepo;
        }


       
        public async Task<LoanDto?> CreateLoanAsync(CreateLoanDto createLoan)
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

                var existingLoanType = await _prequalifiedLoanRepo.GetPreQualifiedLoanByTypeAsync(createLoan.loanType);
                if (existingLoanType == null)
                    throw new NotFoundException($"Loan of type {createLoan.loanType} is not currently available");

                if (createLoan.Amount <= 0)
                    throw new ValidationException("Requested amount must be greater than zero");

                if (createLoan.Amount < existingLoanType.MinAmount || createLoan.Amount > existingLoanType.MaxAmount)
                    throw new ValidationException($"Incorrect amount for the {createLoan.loanType} loan type.");

                var loan = new Loan
                {
                    UserProfileId = AuthUserId,
                    LoanType = createLoan.loanType,
                    RequestedAmount = createLoan.Amount,
                    RequestedDate = DateTime.UtcNow,
                    Status = LoanStatus.Pending,
                };
                await _loanRepository.AddLoanAsync(loan);

                //History record
                var loanHistory = new LoanHistory
                {
                    LoanId = loan.Id,
                    LoanType = loan.LoanType,
                    RequestedAmount = loan.RequestedAmount,
                    RequestedDate = DateTime.UtcNow,
                    Status = loan.Status,
                    UserProfileId = AuthUserId,
                };
                await _loanHistoryRepository.AddLoanHistoryAsync(loanHistory);

                return new LoanDto
                {
                    Id = loan.Id,
                    LoanType = loan.LoanType,
                    RequestedAmount = loan.RequestedAmount,
                    RequestedDate = loan.RequestedDate,
                    Status = loan.Status,
                    UserProfileId = loan.UserProfileId
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

        public async Task<ContinuationResponse<LoanDto>> GetAllLoansAsync(int pageSize, string? continuationToken, LoanStatus? status, string? loanId)
        {
            try
            {
                if (pageSize < 1 || pageSize > 100)
                {
                    throw new NotFoundException("PageSize must be between 1 and 100");
                }
                var (loans, newContinuationToken) = await _loanRepository.GetAllLoansAsync(pageSize, continuationToken, status, loanId);

                var loanDtos = new List<LoanDto>();

                foreach (var loan in loans)
                {
                    loanDtos.Add(new LoanDto
                    {
                        Id = loan.Id,
                        LoanType = loan.LoanType,
                        RequestedAmount = loan.RequestedAmount,
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
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        // Loan Management
        //public async Task<ContinuationResponse<AdminLoanDto>> GetAllLoansAsync(int pageSize, string? continuationToken, LoanStatus? status, string? loanId)
        //{
        //    try
        //    {
        //        if (pageSize < 1 || pageSize > 100)
        //        {
        //            throw new ValidationException("PageSize must be between 1 and 100");
        //        }
        //        var (loans, newContinuationToken) = await _loanRepository.GetAllLoansAsync(pageSize, continuationToken, status, loanId);

        //        var loanDtos = new List<AdminLoanDto>();

        //        foreach (var loan in loans)
        //        {
        //            var user = await _loanRepository.GetUserByIdAsync(loan.UserProfileId);
        //            loanDtos.Add(new AdminLoanDto
        //            {
        //                Id = loan.Id,
        //                LoanType = loan.LoanType,
        //                Amount = loan.Amount,
        //                ApprovedAmount = loan.ApprovedAmount,
        //                Status = loan.Status,
        //                RequestedDate = loan.RequestedDate,
        //                ApprovalDate = loan.ApprovalDate,
        //                UserProfileId = loan.UserProfileId,
        //                UserName = user != null ? $"{user.FirstName} {user.LastName}" : null,
        //            });
        //        }
        //        return new ContinuationResponse<AdminLoanDto>
        //        {
        //            Data = loanDtos,
        //            ContinuationToken = newContinuationToken,
        //            HasMore = !string.IsNullOrEmpty(newContinuationToken)
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new ExternalServiceUnavailableException(
        //            "Service is temporarily unavailable. Please try again later.",
        //            ex
        //        );
        //    }
        //}


        //public async Task<bool> MarkLoanAsPaidAsync(string loanId)
        //{
        //    var loan = await _adminRepository.GetLoanByIdAsync(loanId);
        //    if (loan == null)
        //        return false;

        //    if (loan.Status != LoanStatus.Approved)
        //        throw new InvalidOperationException("Only approved loans can be marked as paid.");

        //    loan.Status = LoanStatus.Paid;
        //    loan.UpdatedDate = DateTime.UtcNow;

        //    var history = new LoanHistory
        //    {
        //        Id = Guid.NewGuid().ToString(),
        //        LoanId = loan.Id,
        //        LoanType = loan.LoanType,
        //        RequestedAmount = loan.Amount,
        //        ApprovedAmount = loan.ApprovedAmount,
        //        RequestedDate = loan.RequestedDate,
        //        ApprovalDate = loan.ApprovalDate,
        //        Status = LoanStatus.Paid,
        //        UserProfileId = loan.UserProfileId,
        //    };
        //    await _adminRepository.UpdateLoanStatusAsync(loanId, LoanStatus.Paid);
        //    await _adminRepository.AddLoanHistoryAsync(history);

        //    return true;
        //}

        //public async Task<AdminLoanDto?> GetLoanByIdAsync(string loanId)
        //{
        //    try
        //    {
        //        var loan = await _loanRepository.GetLoanByIdAsync(loanId);
        //        if (loan == null)
        //            throw new NotFoundException("Loan not found");

        //        var user = await _loanRepository.GetUserByIdAsync(loan.UserProfileId);

        //        return new AdminLoanDto
        //        {
        //            Id = loan.Id,
        //            LoanType = loan.LoanType,
        //            Amount = loan.Amount,
        //            ApprovedAmount = loan.ApprovedAmount,
        //            Status = loan.Status,
        //            RequestedDate = loan.RequestedDate,
        //            ApprovalDate = loan.ApprovalDate,
        //            UserProfileId = loan.UserProfileId,
        //            UserName = user != null ? $"{user.FirstName} {user.LastName}" : null,
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new ExternalServiceUnavailableException(
        //            "Service is temporarily unavailable. Please try again later.",
        //            ex
        //        );
        //    }
        //}


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
                    RequestedAmount = loan.RequestedAmount,
                    RequestedDate = loan.RequestedDate,
                    Status = loan.Status,
                    UserProfileId = loan.UserProfileId
                };
            }
            catch (ValidationException)
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


        public async Task<LoanDto?> UpdateLoanStatusAsync(string loanId, LoanStatus newStatus)
        {
            try
            {
                var loan = await _loanRepository.UpdateLoanStatusAsync(loanId, newStatus);
                if (loan == null)
                {
                    throw new NotFoundException("Loan not found");
                }

                var user = await _userRepository.GetUserByIdAsync(loan.UserProfileId);
                if (user != null)
                {
                    //#if DEBUG
                    //    user.Email = "adesolaopeyemi216@gmail.com";
                    //#endif
                    if (newStatus == LoanStatus.Approved)
                    {
                        await _emailService.SendLoanApprovalEmailAsync(user, loan);
                    }
                    else if (newStatus == LoanStatus.Rejected)
                    {
                        await _emailService.SendLoanRejectionEmailAsync(user, loan);
                    }
                }
                return new LoanDto
                {
                    Id = loan.Id,
                    LoanType = loan.LoanType,
                    RequestedAmount = loan.RequestedAmount,
                    //ApprovedAmount = loan.ApprovedAmount,
                    Status = loan.Status,
                    RequestedDate = loan.RequestedDate,
                    UpdatedDate = loan.UpdatedDate,
                    UserProfileId = loan.UserProfileId,
                    //UserName = user != null ? $"{user.FirstName} {user.LastName}" : null,
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


        public async Task<bool> DeleteLoanAsync(string loanId)
        {
            try
            {
                var deleted = await _loanRepository.DeleteLoanAsync(loanId);
                if (!deleted)
                {
                    throw new NotFoundException("Loan not found.");
                }
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
