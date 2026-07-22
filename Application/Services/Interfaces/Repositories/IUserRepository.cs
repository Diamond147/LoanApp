using Domain.Entities;

namespace Application.Services.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<UserProfile?> GetUserDashboardAsync(string userId);

        Task<bool> AnyAsync();

        Task<UserProfile?> GetUserByEmailAsync(string email);
        Task<UserProfile?> GetUserByMobileAsync(string mobileNumber);
        Task<UserProfile?> SearchUserAsync(string searchTerm);
        Task<IEnumerable<UserProfile>> GetUsersByGenderAsync(string gender);
        Task<IEnumerable<UserProfile>> GetUsersByNationalityAsync(string nationality);

        Task AddUserAsync(UserProfile userProfile);
        Task<(List<UserProfile> Users, string? ContinuationToken)> GetAllUsersDetailsAsync(
            int pageSize = 10,
            string? continuationToken = null,
            string? userId = null,
            string? email = null,
            string? mobileNumber = null,
            string? gender = null,
            string? nationality = null,
            string? searchTerm = null);
        Task<(List<UserProfile> UserProfiles, string? ContinuationToken)> GetAllUsersAsync(int pageSize, string? continuationToken, string? userId = null);
        Task<UserProfile?> GetUserByIdAsync(string userId);
        Task<UserProfile> UpdateUserAsync(UserProfile user);
        Task<UserProfile> PatchUserAsync(UserProfile user);
        Task<UserProfile?> DeleteUserAsync(string userId);
    }
}
