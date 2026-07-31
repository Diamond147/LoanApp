using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;


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

            if (createLoan.loanType != existingLoanType.LoanType)
                throw new ValidationException($"Invalid loan type. Please select a valid loan type.");

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


        public async Task<ContinuationResponse<LoanDto>> GetAllLoansAsync(int pageSize, string? continuationToken, LoanStatus? status, string? loanId)
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


        public async Task<LoanDto?> GetLoanByIdAsync(string loanId, string userId)
        {
            var loan = await _loanRepository.GetLoanByIdAsync(loanId);
            if (loan == null)
                throw new NotFoundException("Loan not found.");

            // Ownership Check: Verify the loan belongs to the requesting user
            if (loan.UserProfileId != userId)
                throw new UnauthorizedException("You are not authorized to view this loan.");

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


        public async Task<LoanDto?> UpdateLoanStatusAsync(string loanId, LoanStatus newStatus)
        {
            var loan = await _loanRepository.GetLoanByIdAsync(loanId);
            if (loan == null)
            {
                throw new NotFoundException("Loan not found.");
            }

            var previousStatus = loan.Status;

            if (previousStatus == newStatus)
            {
                var existingUser = await _userRepository.GetUserByIdAsync(loan.UserProfileId);
                return new LoanDto
                {
                    Id = loan.Id,
                    LoanType = loan.LoanType,
                    RequestedAmount = loan.RequestedAmount,
                    Status = loan.Status,
                    RequestedDate = loan.RequestedDate,
                    UpdatedDate = loan.UpdatedDate,
                    UserProfileId = loan.UserProfileId,
                    //UserName = existingUser != null ? $"{existingUser.FirstName} {existingUser.LastName}" : null
                };
            }

            loan.Status = newStatus;
            loan.UpdatedDate = DateTime.UtcNow;

            await _loanRepository.UpdateLoanAsync(loan);

            var historyEntry = new LoanHistory
            {
                LoanId = loan.Id,
                LoanType = loan.LoanType,
                RequestedAmount = loan.RequestedAmount,
                RequestedDate = loan.RequestedDate,
                Status = newStatus,
                UpdatedDate = DateTime.UtcNow,
                UserProfileId = loan.UserProfileId
            };

            await _loanHistoryRepository.AddLoanHistoryAsync(historyEntry);

            //var user = await _userRepository.GetUserByIdAsync(loan.UserProfileId);
            //if (user == null)
            //{
            //    Console.WriteLine($"[EMAIL FAILED]: Could not find user with UserProfileId = '{loan.UserProfileId}'");
            //}
            //else
            //{
            //    Console.WriteLine($"[EMAIL ATTEMPT]: User found ({user.Email}). Target Status: {newStatus}");

            //    if (newStatus == LoanStatus.Approved)
            //    {
            //        var sent = await _emailService.SendLoanApprovalEmailAsync(user, loan);
            //        Console.WriteLine($"📧 [EMAIL RESULT]: Approval email sent result = {sent}");
            //    }
            //    else if (newStatus == LoanStatus.Rejected)
            //    {
            //        var sent = await _emailService.SendLoanRejectionEmailAsync(user, loan);
            //        Console.WriteLine($"📧 [EMAIL RESULT]: Rejection email sent result = {sent}");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"ℹ️ [EMAIL SKIPPED]: Status '{newStatus}' does not trigger an email.");
            //    }
            //}

            var user = await _userRepository.GetUserByIdAsync(loan.UserProfileId);
            if (user != null)
            {
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
                Status = loan.Status,
                RequestedDate = loan.RequestedDate,
                UpdatedDate = loan.UpdatedDate,
                UserProfileId = loan.UserProfileId,
                //UserName = user != null ? $"{user.FirstName} {user.LastName}" : null
            };
        }


        public async Task<bool> DeleteLoanAsync(string loanId)
        {
            var deleted = await _loanRepository.DeleteLoanAsync(loanId);
            if (!deleted)
            {
                throw new NotFoundException("Loan not found.");
            }
            return deleted;
        }

    }
}
