using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;


namespace Application.Services.Interfaces.Services
{
    public interface IAuthService
    {
        Task<UserProfileDto> CreateUserProfileAsync(CreateUserProfileDto createUserProfileDto);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task LogoutAsync();
    }
}
