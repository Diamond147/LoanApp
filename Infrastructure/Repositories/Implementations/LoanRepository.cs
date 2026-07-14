using Domain.Entities;
using Domain.Enums;
using Infrastructure.DbContexts;
using Application.Services.Interfaces.Repositories;
using Infrastructure.Services.Utilities.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly AppDbContext _context;
        public LoanRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddLoanAsync(Loan loan)
        {
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();
        }


        public async Task<(List<Loan> Loans, string? ContinuationToken)> GetLoansWithContinuationAsync(
            int pageSize,
            string? continuationToken,
            LoanStatus? status,
            string? loanId)
        {
            var query = _context.Loans.AsQueryable();
            if (!string.IsNullOrEmpty(loanId))
            {
                query = query.Where(l => l.Id == loanId);
            }

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status);
            }

            query = query.OrderByDescending(l => l.RequestedDate);

            // Decode continuation token
            var tokenData = ContinuationTokenHelper.Decode(continuationToken);
            int skip = tokenData?.Skip ?? 0;

            // Skip based on decoded token
            query = query.Skip(skip);

            var loans = await query.Take(pageSize + 1).ToListAsync();

            bool hasMore = loans.Count > pageSize;

            if (hasMore)
            {
                loans = loans.Take(pageSize).ToList();
            }

            // Generate next continuation token (base64 encoded)
            string? nextToken = hasMore
                ? ContinuationTokenHelper.Encode(skip + pageSize)
                : null;

            return (loans, nextToken);
        }


        public async Task<bool> HasUnpaidLoanAsync(string userId)
        {
            var loans = await _context.Loans
                .Where(l => l.UserProfileId == userId)
                .ToListAsync();

            if (!loans.Any())
                return false;

            return loans.Any( l => l.Status != LoanStatus.Paid &&
                                   l.Status != LoanStatus.Rejected);
        }


        public async Task<Loan?> GetLoanByIdAsync(string loanId)
        {
            return await _context.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
        }

        public async Task<Loan?> GetApprovedLoanByUserIdAsync(string userId)
        {
            return await _context.Loans
                .Where(l => l.UserProfileId == userId && l.Status == LoanStatus.Approved)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateLoanAsync(Loan loan)
        {
            _context.Loans.Update(loan);
            await _context.SaveChangesAsync();
        }

        public async Task AddLoanHistoryAsync(LoanHistory loanHistory)
        {
            //try
            //{
                _context.LoanHistories.Add(loanHistory);
                await _context.SaveChangesAsync();
            //}
            //catch (DbUpdateException ex)
            //{
            //    // If it already exists or a DB update conflict occurs, detach and ignore the error
            //    _context.Entry(loanHistory).State = EntityState.Detached;
            //}
        }

        public async Task<bool> historyExists(string loanId)
        {
            var history = await _context.LoanHistories
                .Where(h => h.LoanId == loanId && h.Status == LoanStatus.Paid)
                .Select(h => h.Id)
                .FirstOrDefaultAsync();

            return history != null;
        }


        public async Task<List<PreQualifiedLoan>> GetAllPreQualifiedLoansAsync()
        {
            return await _context.PreQualifiedLoans
                .OrderByDescending(p => p.LoanType)
                .ToListAsync();
        }

        public async Task<PreQualifiedLoan?> GetPreQualifiedLoanByTypeAsync(LoanType loanType)
        {
            return await _context.PreQualifiedLoans
                .Where(p => p.LoanType == loanType)
                .FirstOrDefaultAsync();
        }

    }
}
