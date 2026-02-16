using Domain.Entities;
using Domain.Enums;
using Infrastructure.DbContexts;
using Infrastructure.Repositories.Interfaces;
using Infrastructure.Services.Utilities.Helpers;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _context;

        public AdminRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<UserProfile> Users, string? ContinuationToken)> GetAllUsersDetailsAsync(
            int pageSize = 10,
            string? continuationToken = null,
            string? userId = null)
        {
            // Get users 
            var usersQuery = _context.UserProfiles.AsQueryable();

            // Filter by userId if provided
            if (!string.IsNullOrEmpty(userId))
            {
                usersQuery = usersQuery.Where(u => u.Id == userId);
            }

            // Order by SignUpDate (most recent first)
            usersQuery = usersQuery.OrderByDescending(u => u.SignUpDate);

            // Decode continuation token
            var tokenData = ContinuationTokenHelper.Decode(continuationToken);
            int skip = tokenData?.Skip ?? 0;

            // Skip based on decoded token
            usersQuery = usersQuery.Skip(skip);

            // Take pageSize + 1 to check if there are more records
            var users = await usersQuery.Take(pageSize + 1).ToListAsync();

            bool hasMore = users.Count > pageSize;
            if (hasMore)
            {
                users = users.Take(pageSize).ToList();
            }

            // Generate next continuation token (base64 encoded)
            string? nextToken = hasMore
                ? ContinuationTokenHelper.Encode(skip + pageSize)
                : null;

            if (!users.Any())
                return (users, null);

            // Get all user IDs
            var userIds = users.Select(u => u.Id).ToList();

            // Load all loans for these users
            var allLoans = await _context.Loans
                .Where(l => userIds.Contains(l.UserProfileId))
                .ToListAsync();

            // Load all loanHistories
            var allHistories = await _context.LoanHistories
                .Where(lh => allLoans.Select(l => l.Id).Contains(lh.LoanId))
                .ToListAsync();

            // Map loans and histories back to users
            foreach (var user in users)
            {
                user.Loans = await _context.Loans
                    .Where(l => l.UserProfileId == user.Id)
                    .ToListAsync();
                foreach (var loan in user.Loans)
                {
                    loan.LoanHistories = await _context.LoanHistories
                        .Where(lh => lh.LoanId == loan.Id)
                        .ToListAsync();
                }
            }
            return (users, nextToken);
        }


        // User Management
        public async Task<(List<UserProfile> UserProfiles, string? ContinuationToken)> GetUsersWithContinuationAsync(int pageSize, string? continuationToken, string? userId = null)
        {
            var query = _context.UserProfiles.AsQueryable();

            // Filter by userId if provided
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(u => u.Id == userId);
            }

            // Order by SignUpDate (most recent first)
            query = query.OrderByDescending(u => u.SignUpDate);

            // Decode continuation token
            var tokenData = ContinuationTokenHelper.Decode(continuationToken);
            int skip = tokenData?.Skip ?? 0;

            // Skip based on decoded token
            query = query.Skip(skip);

            // Take pageSize + 1 to check if there are more records
            var users = await query.Take(pageSize + 1).ToListAsync();

            bool hasMore = users.Count > pageSize;
            if (hasMore)
            {
                users = users.Take(pageSize).ToList();
            }

            // Generate next continuation token (base64 encoded)
            string? nextToken = hasMore
                ? ContinuationTokenHelper.Encode(skip + pageSize)
                : null;

            return (users, nextToken);
        }           

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.UserProfiles.CountAsync();
        }

        public async Task<UserProfile?> GetUserByIdAsync(string userId)
        {
            return await _context.UserProfiles.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) 
                return false;

            _context.UserProfiles.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }


        // Loan Management
        public async Task<(List<Loan> Loans, string? ContinuationToken)> GetLoansWithContinuationAsync(
            int pageSize,
            string? continuationToken,
            LoanStatus? status,
            string? loanId)
        {
            var query = _context.Loans.AsQueryable();

            // Filter by loanId & status if provided
            if (!string.IsNullOrEmpty(loanId))
                query = query.Where(l => l.Id == loanId);

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

            // Take pageSize + 1 to check if there are more records
            var loans = await query.Take(pageSize + 1).ToListAsync();

            // Check if there are more records
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
                loan.ApprovedAmount = loan.Amount;
                loan.ApprovalDate = DateTime.UtcNow;
            }
            else if (newStatus == LoanStatus.Rejected)
            {
                loan.ApprovedAmount = 0;
                loan.ApprovalDate = null;
            }

            // Create history record
            var history = new LoanHistory
            {
                Id = Guid.NewGuid().ToString(),
                LoanId = loan.Id,
                LoanType = loan.LoanType,
                RequestedAmount = loan.Amount,
                ApprovedAmount = loan.ApprovedAmount,
                RequestedDate = loan.RequestedDate,
                ApprovalDate = loan.ApprovalDate,
                Status = newStatus,
                UserProfileId = loan.UserProfileId,
            };

            await _context.LoanHistories.AddAsync(history);
            await _context.SaveChangesAsync();

            return loan;
        }

        public async Task<Loan?> GetLoanByIdAsync(string loanId)
        {
            return await _context.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
        }

        public async Task<bool> DeleteLoanAsync(string loanId)
        {
            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
            if (loan == null) return false;

            _context.Loans.Remove(loan);
            await _context.SaveChangesAsync();
            return true;
        }


        // PreQualified Management
        public async Task AddPreQualifiedLoanAsync(PreQualifiedLoan preQualifiedLoan)
        {
            _context.PreQualifiedLoans.Add(preQualifiedLoan);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PreQualifiedLoan>> GetAllPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId)
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
                .OrderByDescending(p => p.LoanType)
                .ToListAsync();
        }

        public async Task<PreQualifiedLoan?> GetPreQualifiedLoanByIdAsync(string preQualifiedId)
        {
            return await _context.PreQualifiedLoans.FirstOrDefaultAsync(p => p.Id == preQualifiedId);
        }

        public async Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId)
        {
            var preQualified = await _context.PreQualifiedLoans.FirstOrDefaultAsync(p => p.Id == preQualifiedId);
            if (preQualified == null) return false;

            _context.PreQualifiedLoans.Remove(preQualified);
            await _context.SaveChangesAsync();
            return true;
        }


        // History Management
        public async Task AddLoanHistoryAsync(LoanHistory loanHistory)
        {
            _context.LoanHistories.Add(loanHistory);
            await _context.SaveChangesAsync();
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


        // Dashboard Statistics
        public async Task<AdminDashboardStats> GetDashboardStatsAsync()
        {
            var totalUsers = await _context.UserProfiles.CountAsync();

            var totalLoans = await _context.Loans.CountAsync();
            var pendingLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Pending);
            var approvedLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Approved);
            var rejectedLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Rejected);
            var paidLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Paid);

            var allLoans = await _context.Loans.ToListAsync();
            var totalLoanAmount = allLoans.Sum(l => l.Amount);

            var approvedLoansList = await _context.Loans
                .Where(l => l.Status == LoanStatus.Approved)
                .ToListAsync();
            var totalApprovedAmount = approvedLoansList.Sum(l => l.ApprovedAmount ?? 0);

            var totalLoanHistories = await _context.LoanHistories.CountAsync();

            return new AdminDashboardStats
            {
                TotalUsers = totalUsers,
                TotalLoans = totalLoans,
                PendingLoans = pendingLoans,
                ApprovedLoans = approvedLoans,
                RejectedLoans = rejectedLoans,
                PaidLoans = paidLoans,
                TotalLoanAmount = totalLoanAmount,
                TotalApprovedAmount = totalApprovedAmount,
                TotalLoanHistories = totalLoanHistories,
            };
        }
            
    }
}