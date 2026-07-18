using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;

namespace Application.Services.Interfaces.Services
{
    public interface IUserService
    {
        //Task<UserProfileDto> GetOrCreateUserProfileAsync();
        Task<UserProfileDto> CreateUserProfileAsync(CreateUserProfileDto createUserProfileDto);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task LogoutAsync();
        Task<UserProfileDto> CompleteUserProfileAsync(CompleteProfileDto completeProfileDto);
        Task<UserProfileDto> UpdateUserAsync(UpdateUserProfileDto updateUser);
        Task<UserProfileDto?> GetUserByIdAsync(string userId);
        Task<UserProfileDto> PatchUserAsync(string userId, PatchUserProfileDto patchUser);
        Task<bool> DeleteUserAsync(string userId);
    }
}
