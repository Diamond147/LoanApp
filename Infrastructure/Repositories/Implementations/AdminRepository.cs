using Domain.Entities;
using Domain.Enums;
using Infrastructure.DbContexts;
using Application.Services.Interfaces.Repositories;
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
            var totalLoanAmount = allLoans.Sum(l => l.RequestedAmount);

            var approvedLoansList = await _context.Loans
                .Where(l => l.Status == LoanStatus.Approved)
                .ToListAsync();
            //var totalApprovedAmount = approvedLoansList.Sum(l => l.ApprovedAmount ?? 0);

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
                //TotalApprovedAmount = totalApprovedAmount,
                TotalLoanHistories = totalLoanHistories,
            };
        }
            
    }
}