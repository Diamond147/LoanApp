using Application.DTOs;
using Domain.DTOs.Admin;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;

namespace Application.Services.Interfaces.Services
{
    public interface IUserService
    {
        Task<ContinuationResponse<AllUserDetailsDto>> GetAllUsersDetailsAsync(int pageSize, string? continuationToken, string? userId, string? email, string? mobileNumber, string? gender, string? nationality, string? searchTerm);
        Task<ContinuationResponse<UserProfileDto>> GetAllUsersAsync(int pageSize, string? continuationToken, string? userId);
        Task ChangeUserRoleAsync(string UserId, ChangeRoleDto dto);
        Task<UserProfileDto?> GetUserByIdAsync(string userId);
        Task<UserProfileDto> UpdateUserAsync(UpdateUserProfileDto updateUser);
        Task<UserProfileDto> PatchUserAsync(string userId, PatchUserProfileDto patchUser);
        Task<bool> DeleteUserAsync(string userId);
    }
}
