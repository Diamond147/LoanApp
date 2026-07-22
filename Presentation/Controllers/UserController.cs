using Application.DTOs;
using Application.Services.Implementations;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Users.RequestDto;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize(Roles = "User,Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("all-details")]
        public async Task<IActionResult> GetAllUsersWithDetails(
           [FromQuery] int pageSize = 10,
           [FromQuery] string? continuationToken = null,
           [FromQuery] string? userId = null,
           [FromQuery] string? email = null,
           [FromQuery] string? mobileNumber = null,
           [FromQuery] string? gender = null,
           [FromQuery] string? nationality = null,
           [FromQuery] string? searchTerm = null)
        {
            var result = await _userService.GetAllUsersDetailsAsync(pageSize, continuationToken, userId, email, mobileNumber, gender, nationality, searchTerm);
            return Ok(result);
        }

        //[HttpGet("users")]
        //public async Task<IActionResult> GetAllUsers(
        //    [FromQuery] int pageSize = 10,
        //    [FromQuery] string? continuationToken = null,
        //    [FromQuery] string? userId = null)
        //{
        //    var result = await _userService.GetUsersWithContinuationAsync(pageSize, continuationToken, userId);
        //    return Ok(result);
        //}


        [Authorize(Roles = "Admin")]
        [HttpPost("{userId}/role")]
        public async Task<IActionResult> ChangeRole(string userId, [FromBody] ChangeRoleDto dto)
        {
            await _userService.ChangeUserRoleAsync(userId, dto);
            return Ok(new { message = "User role successfully updated." });
        }


        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            var result = await _userService.GetUserByIdAsync(userId);
            return Ok(result);
        }


        [HttpPatch("{userId}")]
        public async Task<IActionResult> PatchUser(string userId, [FromBody] PatchUserProfileDto PatchUser)
        {
            var result = await _userService.PatchUserAsync(userId, PatchUser);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var success = await _userService.DeleteUserAsync(userId);
            return NoContent();
        }


        //[HttpPut("userprofiles/complete")]
        //public async Task<IActionResult> CompleteUserProfile([FromBody] CompleteProfileDto dto)
        //{
        //    var profile = await _userService.CompleteUserProfileAsync(dto);
        //    return Ok(profile);
        //}


        //[HttpPut("users/update")]
        //public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto dto)
        //{
    //        var profile = await _userService.UpdateUserAsync(dto);
    //        return Ok(profile);
        //}


    }
}
