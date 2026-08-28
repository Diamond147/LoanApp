using Application.Exceptions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.ResponseDto;
using Application.Services.Interfaces.ExternalServices;
using AutoMapper;


namespace Application.Services.Implementations
{
    public class LoanHistoryService : ILoanHistoryService
    {
        private readonly ILoanHistoryRepository _loanHistoryRepository;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;

        public LoanHistoryService(ILoanHistoryRepository lonHistoryRepository, ICacheService cacheService, IMapper mapper)
        {
            _loanHistoryRepository = lonHistoryRepository;
            _cacheService = cacheService;
            _mapper = mapper;
        }


        public async Task<IEnumerable<LoanHistoryDto>> GetLoanHistoryByLoanIdAsync(string loanId)
        {
            string cacheKey = $"loanhistory:loan:{loanId}";

            var list = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var histories = await _loanHistoryRepository.GetLoanHistoryByLoanIdAsync(loanId);

                    return histories.Select(h => _mapper.Map<LoanHistoryDto>(h)).ToList();
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

                    return _mapper.Map<LoanHistoryDto>(history);
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
