using Domain.Entities;

namespace Infrastructure.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task AddUserAsync(UserProfile userProfile);
        Task<UserProfile?> GetUserDashboardAsync(string userId);
        Task<UserProfile?> GetUserByIdAsync(string userId);
        Task<UserProfile?> GetUserByEmailAsync(string email);
        Task<UserProfile?> GetUserByMobileAsync(string mobileNumber);
        Task<UserProfile?> SearchUserAsync(string searchTerm);
        Task<IEnumerable<UserProfile>> GetUsersByGenderAsync(string gender);
        Task<IEnumerable<UserProfile>> GetUsersByNationalityAsync(string nationality);
        Task<UserProfile> UpdateUserProfileAsync(UserProfile user);
        Task<UserProfile> PatchUserAsync(UserProfile user);
        Task<UserProfile?> DeleteUserAsync(string userId);
    }
}