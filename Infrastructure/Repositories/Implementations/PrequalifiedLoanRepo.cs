using Application.Services.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories.Implementations
{
    public class PrequalifiedLoanRepo : IPrequalifiedLoanRepo
    {
        private readonly AppDbContext _context;

        public PrequalifiedLoanRepo(AppDbContext context)
        {
            _context = context;
        }


        public async Task AddPreQualifiedLoanAsync(PreQualifiedLoan preQualifiedLoan)
        {
            _context.PreQualifiedLoans.Add(preQualifiedLoan);
            await _context.SaveChangesAsync();
        }


        public async Task<List<PreQualifiedLoan>> GetPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId)
        {
            var query = _context.PreQualifiedLoans.AsQueryable();
            if (!string.IsNullOrEmpty(preQualifiedId))
            {
                query = query.Where(p => p.Id == preQualifiedId);
            }
            if (loanType.HasValue)
            {
                query = query.Where(p => p.LoanType == loanType);
            }
            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PreQualifiedLoan>> GetAllPreQualifiedLoansAsync()
        {
            return await _context.PreQualifiedLoans
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }


        public async Task<PreQualifiedLoan?> GetPreQualifiedLoanByIdAsync(string preQualifiedId)
        {
            return await _context.PreQualifiedLoans.FirstOrDefaultAsync(p => p.Id == preQualifiedId);
        }


        public async Task<PreQualifiedLoan?> GetPreQualifiedLoanByTypeAsync(LoanType loanType)
        {
            return await _context.PreQualifiedLoans
                .Where(p => p.LoanType == loanType)
                .FirstOrDefaultAsync();
        }


        public async Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId)
        {
            var preQualified = await _context.PreQualifiedLoans.FirstOrDefaultAsync(p => p.Id == preQualifiedId);
            if (preQualified == null) return false;

            _context.PreQualifiedLoans.Remove(preQualified);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
