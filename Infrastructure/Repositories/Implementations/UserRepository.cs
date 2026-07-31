using Application.Services.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.DbContexts;
using Infrastructure.Services.Utilities.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }



        public async Task<bool> AnyAsync()
        {
            return await _context.UserProfiles.AnyAsync();
        }


        public async Task AddUserAsync(UserProfile userProfile)
        {
            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync();
        }


        //Retrieves the user with all related data(Loans, LoanHistories).
        public async Task<UserProfile?> GetUserDashboardAsync(string userId)
        {
            var user = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return null;

            // Load related data separately (not using .Include())
            user.Loans = await _context.Loans
                .Where(l => l.UserProfileId == user.Id)
                .ToListAsync();

            foreach(var loan in user.Loans)
            {
                loan.LoanHistories = await _context.LoanHistories
                .Where(lh => lh.LoanId == loan.Id)
                .ToListAsync();
            }
            
            return user;
        }


        // All Details - Users + Loans + LoanHistories
        public async Task<(List<UserProfile> Users, string? ContinuationToken)> GetAllUsersDetailsAsync(
            int pageSize = 10,
            string? continuationToken = null,
            string? userId = null,
            string? email = null,
            string? mobileNumber = null,
            string? gender = null,
            string? nationality = null,
            string? searchTerm = null)
        {
            // Get users 
            var usersQuery = _context.UserProfiles.AsQueryable();

            // Filtering if provided
            if (!string.IsNullOrEmpty(userId))
                usersQuery = usersQuery.Where(u => u.Id == userId);

            if (!string.IsNullOrEmpty(email))
                usersQuery = usersQuery.Where(u => u.Email == email);

            if (!string.IsNullOrEmpty(mobileNumber))
                usersQuery = usersQuery.Where(u => u.MobileNumber == mobileNumber);

            if (!string.IsNullOrEmpty(gender))
                usersQuery = usersQuery.Where(u => u.Gender == gender);

            if (!string.IsNullOrEmpty(nationality))
                usersQuery = usersQuery.Where(u => u.Nationality == nationality);

            if (!string.IsNullOrEmpty(searchTerm))
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.Contains(searchTerm) ||
                    u.LastName.Contains(searchTerm) ||
                    u.Email.Contains(searchTerm) ||
                    u.MobileNumber != null && u.MobileNumber.Contains(searchTerm) ||
                    u.Gender != null && u.Gender.Contains(searchTerm) ||
                    u.Nationality != null && u.Nationality.Contains(searchTerm));

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
        public async Task<(List<UserProfile> UserProfiles, string? ContinuationToken)> GetAllUsersAsync(int pageSize, string? continuationToken, string? userId = null)
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
            var user = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return null;

            user.Loans = await _context.Loans
                .Where(l => l.UserProfileId == user.Id)
                .ToListAsync();

            foreach (var loan in user.Loans)
            {
                loan.LoanHistories = await _context.LoanHistories
                .Where(lh => lh.LoanId == loan.Id)
                .ToListAsync();
            }

            return user;
        }

        public async Task<UserProfile?> GetUserByEmailAsync(string email)
        {
            var user = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return null;

            user.Loans = await _context.Loans
                .Where(l => l.UserProfileId == user.Id)
                .ToListAsync();

            foreach (var loan in user.Loans)
            {
                loan.LoanHistories = await _context.LoanHistories
                .Where(lh => lh.LoanId == loan.Id)
                .ToListAsync();
            }

            return user;
        }
        public async Task<UserProfile?> GetUserByMobileAsync(string mobileNumber)
        {
            var user = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
            if (user == null)
                return null;

            user.Loans = await _context.Loans
                .Where(l => l.UserProfileId == user.Id)
                .ToListAsync();

            foreach (var loan in user.Loans)
            {
                loan.LoanHistories = await _context.LoanHistories
                .Where(lh => lh.LoanId == loan.Id)
                .ToListAsync();
            }

            return user;
        }

        public async Task<UserProfile?> SearchUserAsync(string searchTerm)
        {
            var lowerSearchTerm = searchTerm.ToLower();

            var user = await _context.UserProfiles
                .FirstOrDefaultAsync(u =>
                u.FirstName.Contains(searchTerm) ||
                u.LastName.Contains(searchTerm) ||
                u.Email.Contains(searchTerm) ||
                u.MobileNumber != null && u.MobileNumber.Contains(searchTerm) ||
                u.Gender != null && u.Gender.Contains(searchTerm) ||
                u.Nationality != null && u.Nationality.Contains(searchTerm));

            if (user == null)
                return null;
            
            user.Loans = await _context.Loans
                    .Where(l => l.UserProfileId == user.Id)
                    .ToListAsync();

            foreach (var loan in user.Loans)
            {
                loan.LoanHistories = await _context.LoanHistories
                .Where(lh => lh.LoanId == loan.Id)
                .ToListAsync();
            }

            return user;
        }

        public async Task<IEnumerable<UserProfile>> GetUsersByGenderAsync(string gender)
        {
            var users = await _context.UserProfiles
                .Where(u => u.Gender == gender)
                .ToListAsync();

            if (!users.Any())
                return users;

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
            return users;
        }
        public async Task<IEnumerable<UserProfile>> GetUsersByNationalityAsync(string nationality)
        {
            var users = await _context.UserProfiles
                .Where(u => u.Nationality == nationality)
                .ToListAsync();

            if (!users.Any())
                return users;

            foreach (var user in users)
            {
                user.Loans = await _context.Loans
                    .Where(l => l.UserProfileId == user.Id)
                    .ToListAsync();

                foreach(var loan in user.Loans)
                {
                    loan.LoanHistories = await _context.LoanHistories
                    .Where(lh => lh.LoanId == loan.Id)
                    .ToListAsync();
                }
            }
            return users;
        }
        public async Task<UserProfile> UpdateUserAsync(UserProfile user)
        {
            _context.UserProfiles.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<UserProfile> PatchUserAsync(UserProfile user)
        {
            _context.UserProfiles.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<UserProfile?> DeleteUserAsync(string userId)
        {
            var user = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return null;
            }
            _context.UserProfiles.Remove(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
