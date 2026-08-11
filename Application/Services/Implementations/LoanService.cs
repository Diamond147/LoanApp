using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces.ExternalServices;
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
        private readonly ICacheService _cacheService;


        public LoanService(ILoanRepository loanRepository, IHttpContextAccessor httpContextAccessor, IUserRepository userRepository, IEmailService emailService, ILoanHistoryRepository loanHistoryRepository, IPrequalifiedLoanRepo prequalifiedLoanRepo, ICacheService cacheService)
        {
            _loanRepository = loanRepository;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _emailService = emailService;
            _loanHistoryRepository = loanHistoryRepository;
            _prequalifiedLoanRepo = prequalifiedLoanRepo;
            _cacheService = cacheService;
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

            // Execute ONLY after successful DB saves
            // Invalidate this specific user's cached loan list so their UI updates immediately
            await _cacheService.RemoveAsync($"loans:user:{AuthUserId}");

            // Invalidate admin/global cached loan lists so the new loan shows up on admin dashboards
            await _cacheService.RemoveByPrefixAsync("loans:all:");

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


        public async Task<ContinuationResponse<LoanDto>> GetAllLoansAsync(
            int pageSize,
            string? continuationToken,
            LoanStatus? status,
            string? loanId)
        {
            if (pageSize < 1 || pageSize > 100)
            {
                throw new NotFoundException("PageSize must be between 1 and 100");
            }

            // Build a unique cache key incorporating all filter parameters
            string cacheKey = $"loans:all:page={pageSize}:token={continuationToken ?? "none"}:status={status?.ToString() ?? "all"}:id={loanId ?? "none"}";

            // Use GetOrSetAsync (Cache-Aside Pattern)
            Console.WriteLine("Attempting to cache...");
            var result = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    // Fetch from Repository on Cache Miss
                    var (loans, newContinuationToken) = await _loanRepository.GetAllLoansAsync(pageSize, continuationToken, status, loanId);

                    var loanDtos = loans.Select(loan => new LoanDto
                    {
                        Id = loan.Id,
                        LoanType = loan.LoanType,
                        RequestedAmount = loan.RequestedAmount,
                        Status = loan.Status,
                        RequestedDate = loan.RequestedDate,
                        UserProfileId = loan.UserProfileId,
                    }).ToList();

                    return new ContinuationResponse<LoanDto>
                    {
                        Data = loanDtos,
                        ContinuationToken = newContinuationToken,
                        HasMore = !string.IsNullOrEmpty(newContinuationToken)
                    };
                },
                expirationTime: TimeSpan.FromMinutes(15) // Cache list results for 15 minutes
            );
            Console.WriteLine("Cache set complete");

            return result ?? new ContinuationResponse<LoanDto>();
        }


        public async Task<LoanDto?> GetLoanByIdAsync(string loanId, string userId)
        {
            string cacheKey = $"loans:id:{loanId}";

            var loan = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var existingLoan = await _loanRepository.GetLoanByIdAsync(loanId);
                    if (existingLoan == null)
                        throw new NotFoundException("Loan not found.");


                    return new LoanDto
                    {
                        Id = existingLoan.Id,
                        LoanType = existingLoan.LoanType,
                        RequestedAmount = existingLoan.RequestedAmount,
                        RequestedDate = existingLoan.RequestedDate,
                        Status = existingLoan.Status,
                        UserProfileId = existingLoan.UserProfileId
                    };
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

            if (loan == null)
                throw new NotFoundException("Loan not found.");

            // Ownership Check: Verify the loan belongs to the requesting user
            if (loan.UserProfileId != userId)
                throw new UnauthorizedException("You are not authorized to view this loan.");

            return loan;
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

            await _cacheService.RemoveAsync($"loans:id:{loanId}");
            await _cacheService.RemoveAsync($"loans:user:{loan.UserProfileId}");
            await _cacheService.RemoveByPrefixAsync("loans:all:");

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
            var loan = await _loanRepository.GetLoanByIdAsync(loanId);
            if (loan == null)
            { 
                throw new NotFoundException("Loan not found.");
            }

            var deleted = await _loanRepository.DeleteLoanAsync(loanId);
            if (deleted)
            {
                await _cacheService.RemoveAsync($"loans:id:{loanId}");
                await _cacheService.RemoveAsync($"loans:user:{loan.UserProfileId}");
                await _cacheService.RemoveByPrefixAsync("loans:all:");
            }

            return deleted;
        }

    }
}
