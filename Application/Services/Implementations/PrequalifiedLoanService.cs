using Application.Exceptions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;
using Application.Services.Interfaces.ExternalServices;
using AutoMapper;

namespace Application.Services.Implementations
{
    public class PrequalifiedLoanService : IPrequalifiedLoanService
    {
        private readonly IPrequalifiedLoanRepo _prequalifiedLoanRepo;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;

        public PrequalifiedLoanService(IPrequalifiedLoanRepo prequalifiedLoanRepo, ICacheService cacheService, IMapper mapper)
        {
            _prequalifiedLoanRepo = prequalifiedLoanRepo;
            _cacheService = cacheService;
            _mapper = mapper;
        }


        public async Task<PreQualifiedLoanDto?> CreatePreQualifiedLoanAsync(CreatePreQualifiedLoanDto createPqLoan)
        {
            var preQualifiedLoan = _mapper.Map<PreQualifiedLoan>(createPqLoan);
            preQualifiedLoan.CreatedAt = DateTime.UtcNow;

            await _prequalifiedLoanRepo.AddPreQualifiedLoanAsync(preQualifiedLoan);

            // Invalidate cached lists of prequalified loans
            await _cacheService.RemoveByPrefixAsync("prequalified:");

            return _mapper.Map<PreQualifiedLoanDto>(preQualifiedLoan);
        }


        public async Task<List<PreQualifiedLoanDto>> GetPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId)
        {
            string cacheKey = $"prequalified:filter:type={(loanType?.ToString()??"all")}:id={preQualifiedId ?? "none"}";

            var list = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var allPreQualified = await _prequalifiedLoanRepo.GetPreQualifiedLoansAsync(loanType, preQualifiedId);
                    if (allPreQualified == null)
                        return new List<PreQualifiedLoanDto>();

                    return allPreQualified.Select(p => _mapper.Map<PreQualifiedLoanDto>(p)).ToList();
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

            return list ?? new List<PreQualifiedLoanDto>();
        }


        public async Task<List<PreQualifiedLoanDto>> GetAllPreQualifiedLoansAsync()
        {
            string cacheKey = "prequalified:all";

            var list = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var preQualifiedLoans = await _prequalifiedLoanRepo.GetAllPreQualifiedLoansAsync();

                    return preQualifiedLoans.Select(p => _mapper.Map<PreQualifiedLoanDto>(p)).ToList();
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

            return list ?? new List<PreQualifiedLoanDto>();
        }


        public async Task<PreQualifiedLoanDto?> GetPreQualifiedLoanByIdAsync(string preQualifiedId)
        {
            string cacheKey = $"prequalified:id:{preQualifiedId}";

            var dto = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var preQualified = await _prequalifiedLoanRepo.GetPreQualifiedLoanByIdAsync(preQualifiedId);
                    if (preQualified == null)
                        throw new NotFoundException("PreQualified not found");

                    return _mapper.Map<PreQualifiedLoanDto>(preQualified);
                },
                expirationTime: TimeSpan.FromMinutes(15)
            );

            return dto;
        }


        public async Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId)
        {
            var deleted = await _prequalifiedLoanRepo.DeletePreQualifiedLoanAsync(preQualifiedId);
            if (!deleted)
                throw new NotFoundException("PreQualifiedLoan not found");

            // Invalidate caches
            await _cacheService.RemoveAsync($"prequalified:id:{preQualifiedId}");
            await _cacheService.RemoveByPrefixAsync("prequalified:");

            return deleted;
        }
    }
}
