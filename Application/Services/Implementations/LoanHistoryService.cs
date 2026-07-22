using Application.Exceptions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.ResponseDto;


namespace Application.Services.Implementations
{
    public class LoanHistoryService : ILoanHistoryService
    {
        private readonly ILoanHistoryRepository _loanHistoryRepository;

        public LoanHistoryService(ILoanHistoryRepository lonHistoryRepository)
        {
            _loanHistoryRepository = lonHistoryRepository;
        }


        public async Task<IEnumerable<LoanHistoryDto>> GetLoanHistoryByLoanIdAsync(string loanId)
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
            });
        }

        public async Task<LoanHistoryDto?> GetLoanHistoryByHistoryIdAsync(string historyId)
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
        }


        public async Task<bool> DeleteLoanHistoryAsync(string loanHistoryId)
        {
            try
            {
                var deleted = await _loanHistoryRepository.DeleteLoanHistoryAsync(loanHistoryId);
                if (!deleted)
                    throw new NotFoundException("Loan history not found");

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
