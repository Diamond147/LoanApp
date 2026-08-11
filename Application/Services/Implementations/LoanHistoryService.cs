using Application.Exceptions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.ResponseDto;
using Application.Services.Interfaces.ExternalServices;


namespace Application.Services.Implementations
{
    public class LoanHistoryService : ILoanHistoryService
    {
        private readonly ILoanHistoryRepository _loanHistoryRepository;
        private readonly ICacheService _cacheService;

        public LoanHistoryService(ILoanHistoryRepository lonHistoryRepository, ICacheService cacheService)
        {
            _loanHistoryRepository = lonHistoryRepository;
            _cacheService = cacheService;
        }


        public async Task<IEnumerable<LoanHistoryDto>> GetLoanHistoryByLoanIdAsync(string loanId)
        {
            string cacheKey = $"loanhistory:loan:{loanId}";

            var list = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var histories = await _loanHistoryRepository.GetLoanHistoryByLoanIdAsync(loanId);
                    return histories.Select(h => new LoanHistoryDto
                    {
                        Id = h.Id,
                        LoanId = h.LoanId,
                        LoanType = h.LoanType,
                        Status = h.Status,
                        RequestedAmount = h.RequestedAmount,
                        RequestedDate = h.RequestedDate,
                        UserProfileId = h.UserProfileId,
                    }).ToList();
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

            return list ?? Enumerable.Empty<LoanHistoryDto>();
        }

        public async Task<LoanHistoryDto?> GetLoanHistoryByHistoryIdAsync(string historyId)
        {
            string cacheKey = $"loanhistory:id:{historyId}";

            var dto = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var history = await _loanHistoryRepository.GetLoanHistoryByHistoryIdAsync(historyId);
                    if (history == null) return null;

                    return new LoanHistoryDto
                    {
                        Id = history.Id,
                        LoanId = history.LoanId,
                        LoanType = history.LoanType,
                        Status = history.Status,
                        RequestedAmount = history.RequestedAmount,
                        RequestedDate = history.RequestedDate,
                        UserProfileId = history.UserProfileId,
                    };
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

            return dto;
        }


        public async Task<bool> DeleteLoanHistoryAsync(string loanHistoryId)
        {
            var deleted = await _loanHistoryRepository.DeleteLoanHistoryAsync(loanHistoryId);
            if (!deleted)
                throw new NotFoundException("Loan history not found");

            // Invalidate cache for this history id and related loan history lists
            await _cacheService.RemoveAsync($"loanhistory:id:{loanHistoryId}");
            await _cacheService.RemoveByPrefixAsync("loanhistory:loan:");

            return deleted;
        }
    }
}
