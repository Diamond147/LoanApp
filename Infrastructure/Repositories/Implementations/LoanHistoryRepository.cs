using Domain.Entities;
using Domain.Enums;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Application.Services.Interfaces.Repositories;


namespace Infrastructure.Repositories.Implementations
{
    public class LoanHistoryRepository : ILoanHistoryRepository
    {
        private readonly AppDbContext _context;

        public LoanHistoryRepository(AppDbContext context)
        {
            _context = context;
        }


        public async Task AddLoanHistoryAsync(LoanHistory loanHistory)
        {
            _context.LoanHistories.Add(loanHistory);
            await _context.SaveChangesAsync();
        }


        // Fetches all history logs associated with a specific loan ID
        public async Task<IEnumerable<LoanHistory>> GetLoanHistoryByLoanIdAsync(string loanId)
        {
            return await _context.LoanHistories
                .Where(h => h.LoanId == loanId)
                .OrderByDescending(h => h.RequestedDate)
                .ToListAsync();
        }


        public async Task<LoanHistory?> GetLoanHistoryByHistoryIdAsync(string historyId)
        {
            return await _context.LoanHistories.FirstOrDefaultAsync(h => h.Id == historyId);
        }


        public async Task<bool> historyExists(string loanId)
        {
            var history = await _context.LoanHistories
                .Where(h => h.LoanId == loanId && h.Status == LoanStatus.Paid)
                .Select(h => h.Id)
                .FirstOrDefaultAsync();

            return history != null;
        }


        public async Task<bool> DeleteLoanHistoryAsync(string loanHistoryId)
        {
            var loanHistory = await _context.LoanHistories
                .FirstOrDefaultAsync(lh => lh.Id == loanHistoryId);
            if (loanHistory == null)
                return false;

            _context.LoanHistories.Remove(loanHistory);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
