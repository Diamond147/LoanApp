using Domain.Entities;
using Infrastructure.DbContexts;
using Application.Services.Interfaces.Repositories;
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
