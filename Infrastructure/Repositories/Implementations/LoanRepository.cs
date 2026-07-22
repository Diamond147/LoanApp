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


        public async Task<(List<Loan> Loans, string? ContinuationToken)> GetAllLoansAsync(
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


        public async Task<bool> HasPaidLoanAsync(string userId)
        {
            return await _context.Loans
                .AnyAsync(l => l.UserProfileId == userId && l.Status == LoanStatus.Paid);
        }

        public async Task<List<Loan>> GetPaidLoansAsync(string userId)
        {
            return await _context.Loans
                .Where(l => l.UserProfileId == userId && l.Status == LoanStatus.Paid)
                .ToListAsync();
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


        public async Task<Loan?> UpdateLoanStatusAsync(string loanId, LoanStatus newStatus)
        {
            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
            if (loan == null)
                return null;

            // Don't update if already in the same status
            if (loan.Status == newStatus)
                return loan;

            if (newStatus == LoanStatus.Paid && loan.Status != LoanStatus.Approved)
            {
                throw new InvalidOperationException("Only approved loans can be marked as paid.");
            }

            // Now update the status
            loan.Status = newStatus;
            loan.UpdatedDate = DateTime.UtcNow;

            if (newStatus == LoanStatus.Approved)
            {
                loan.RequestedAmount = loan.RequestedAmount;
                loan.UpdatedDate = DateTime.UtcNow;
            }
            else if (newStatus == LoanStatus.Rejected)
            {
                loan.RequestedAmount = 0;
                loan.UpdatedDate = null;
            }

            // Create history record
            var history = new LoanHistory
            {
                Id = Guid.NewGuid().ToString(),
                LoanId = loan.Id,
                LoanType = loan.LoanType,
                RequestedAmount = loan.RequestedAmount,
                //ApprovedAmount = loan.ApprovedAmount,
                RequestedDate = loan.RequestedDate,
                UpdatedDate = loan.UpdatedDate,
                Status = newStatus,
                UserProfileId = loan.UserProfileId,
            };

            await _context.LoanHistories.AddAsync(history);
            await _context.SaveChangesAsync();

            return loan;
        }


        public async Task<bool> DeleteLoanAsync(string loanId)
        {
            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
            if (loan == null) return false;

            _context.Loans.Remove(loan);
            await _context.SaveChangesAsync();
            return true;
        }




        // LoanHistory
        //public async Task AddLoanHistoryAsync(LoanHistory loanHistory)
        //{
        //    _context.LoanHistories.Add(loanHistory);
        //    await _context.SaveChangesAsync();
        //}

        //public async Task<bool> historyExists(string loanId)
        //{
        //    var history = await _context.LoanHistories
        //        .Where(h => h.LoanId == loanId && h.Status == LoanStatus.Paid)
        //        .Select(h => h.Id)
        //        .FirstOrDefaultAsync();

        //    return history != null;
        //}



        //public async Task<List<PreQualifiedLoan>> GetAllPreQualifiedLoansAsync()
        //{
        //    return await _context.PreQualifiedLoans
        //        .OrderByDescending(p => p.LoanType)
        //        .ToListAsync();
        //}

        //public async Task<PreQualifiedLoan?> GetPreQualifiedLoanByTypeAsync(LoanType loanType)
        //{
        //    return await _context.PreQualifiedLoans
        //        .Where(p => p.LoanType == loanType)
        //        .FirstOrDefaultAsync();
        //}

    }
}
