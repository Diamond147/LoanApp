using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;

namespace Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto> GetOrCreateUserProfileAsync();
        Task<UserProfileDto> CompleteUserProfileAsync(CompleteProfileDto completeProfileDto);
        Task<UserProfileDto> UpdateUserAsync(UpdateUserProfileDto updateUser);
        Task<UserProfileDto?> GetUserByIdAsync(string userId);
        Task<UserProfileDto> PatchUserAsync(string userId, PatchUserProfileDto patchUser);
        Task<bool> DeleteUserAsync(string userId);
    }
}
