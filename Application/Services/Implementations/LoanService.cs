using Application.DTOs;
using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using AutoMapper;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;
using Domain.Helpers;
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
        private readonly IMapper _mapper;


        public LoanService(ILoanRepository loanRepository, IHttpContextAccessor httpContextAccessor, IUserRepository userRepository, IEmailService emailService, ILoanHistoryRepository loanHistoryRepository, IPrequalifiedLoanRepo prequalifiedLoanRepo, ICacheService cacheService, IMapper mapper)
        {
            _loanRepository = loanRepository;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _emailService = emailService;
            _loanHistoryRepository = loanHistoryRepository;
            _prequalifiedLoanRepo = prequalifiedLoanRepo;
            _cacheService = cacheService;
            _mapper = mapper;
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

            if (createLoan.RequestedAmount <= 0)
                throw new ValidationException("Requested amount must be greater than zero");

            if (createLoan.RequestedAmount < existingLoanType.MinAmount || createLoan.RequestedAmount > existingLoanType.MaxAmount)
                throw new ValidationException($"Incorrect amount for the {createLoan.loanType} loan type.");

            if (createLoan.loanType != existingLoanType.LoanType)
                throw new ValidationException($"Invalid loan type. Please select a valid loan type.");

            var loan = _mapper.Map<Loan>(createLoan);

            loan.UserProfileId = AuthUserId;
            loan.Status = LoanStatus.Pending;
            loan.InterestRate = existingLoanType.InterestRate;
            loan.AccruedInterest = 0m;
            loan.PrincipalBalance = createLoan.RequestedAmount;
            loan.RequestedDate = DateTime.UtcNow;

            await _loanRepository.AddLoanAsync(loan);

            //History record
            var loanHistory = _mapper.Map<LoanHistory>(loan);

            await _loanHistoryRepository.AddLoanHistoryAsync(loanHistory);

            // Execute ONLY after successful DB saves
            // Invalidate this specific user's cached loan list so their UI updates immediately
            await _cacheService.RemoveAsync($"loans:user:{AuthUserId}");

            // Invalidate admin/global cached loan lists so the new loan shows up on admin dashboards
            await _cacheService.RemoveByPrefixAsync("loans:all:");

            return _mapper.Map<LoanDto>(loan);
        }


        public async Task<ContinuationResponse<LoanDto>> GetAllLoansAsync(int pageSize, string? continuationToken, LoanStatus? status, string? loanId)
        {
            if (pageSize < 1 || pageSize > 100)
                throw new NotFoundException("PageSize must be between 1 and 100");

            string cacheKey = $"loans:all:page={pageSize}:token={continuationToken ?? "none"}:status={status?.ToString() ?? "all"}:id={loanId ?? "none"}";

            var result = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var (loans, newContinuationToken) = await _loanRepository.GetAllLoansAsync(pageSize, continuationToken, status, loanId);

                    var loanDtos = new List<LoanDto>();
                    foreach (var loan in loans)
                    {
                        var (projectedAccrued, _) = LoanInterestCalculator.CalculateProjectedAccrual(loan, DateTime.UtcNow);

                        // Map entity -> DTO then override projected accrued interest
                        var dto = _mapper.Map<LoanDto>(loan);
                        dto.AccruedInterest = projectedAccrued;
                        loanDtos.Add(dto);
                    }

                    return new ContinuationResponse<LoanDto>
                    {
                        Data = loanDtos,
                        ContinuationToken = newContinuationToken,
                        HasMore = !string.IsNullOrEmpty(newContinuationToken)
                    };
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

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

                    var (projectedAccrued, _) = LoanInterestCalculator.CalculateProjectedAccrual(existingLoan, DateTime.UtcNow);

                    // Map entity to DTO and set projected accrued interest
                    var dto = _mapper.Map<LoanDto>(existingLoan);
                    dto.AccruedInterest = projectedAccrued;

                    return dto;
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

            if (loan == null)
                throw new NotFoundException("Loan not found.");

            if (loan.UserProfileId != userId)
                throw new UnauthorizedException("You are not authorized to view this loan.");

            return loan;
        }


        public async Task<LoanDto?> UpdateLoanStatusAsync(string loanId, UpdateLoanStatusDto newStatus)
        {
            var loan = await _loanRepository.GetLoanByIdAsync(loanId);
            if (loan == null)
            {
                throw new NotFoundException("Loan not found.");
            }

            var previousStatus = loan.Status;

            if (previousStatus == newStatus.NewStatus)
            {
                var existingUser = await _userRepository.GetUserByIdAsync(loan.UserProfileId);

                return _mapper.Map<LoanDto>(loan);
            }

            loan.Status = newStatus.NewStatus;
            loan.UpdatedDate = DateTime.UtcNow;

            // If approving, initialize interest-related fields
            if (newStatus.NewStatus == LoanStatus.Approved)
            {
                var pq = await _prequalifiedLoanRepo.GetPreQualifiedLoanByTypeAsync(loan.LoanType);

                loan.InterestRate = pq?.InterestRate ?? 0.5m; // default 50%
                loan.PrincipalBalance = loan.PrincipalBalance; // user-requested amount becomes principal balance
                loan.AccruedInterest = 0m;
                loan.ApprovalDate = DateTime.UtcNow;
                loan.LastInterestAccrualDate = DateTime.UtcNow;
            }

            await _loanRepository.UpdateLoanAsync(loan);

            var historyEntry = _mapper.Map<LoanHistory>(loan);
            historyEntry.Status = newStatus.NewStatus;
            historyEntry.UpdatedDate = DateTime.UtcNow;

            await _loanHistoryRepository.AddLoanHistoryAsync(historyEntry);

            await _cacheService.RemoveAsync($"loans:id:{loanId}");
            await _cacheService.RemoveAsync($"loans:user:{loan.UserProfileId}");
            await _cacheService.RemoveByPrefixAsync("loans:all:");

            var user = await _userRepository.GetUserByIdAsync(loan.UserProfileId);
            if (user != null)
            {
                if (newStatus.NewStatus == LoanStatus.Approved)
                {
                    await _emailService.SendLoanApprovalEmailAsync(user, loan);
                }
                else if (newStatus.NewStatus == LoanStatus.Rejected)
                {
                    await _emailService.SendLoanRejectionEmailAsync(user, loan);
                }
            }

            var result = _mapper.Map<LoanDto>(loan);
            return result;
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
