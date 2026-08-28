using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }


        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> CreateUserProfile([FromBody] CreateUserProfileDto dto)
        {
            var userProfile = await _authService.CreateUserProfileAsync(dto);
            return CreatedAtAction(nameof(CreateUserProfile), new { id = userProfile?.Id }, new { message = "User profile created successfully", data = userProfile });
        }


        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(new { message = "Login successful", data = result });
        }


        [Authorize(Roles = "User,Admin")]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
