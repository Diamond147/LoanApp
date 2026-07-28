using Domain.Entities;
using System.Security.Claims;


namespace Application.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static UserClaimsInfo GetUserInfo(this ClaimsPrincipal user)
        {
            // Use standard ClaimTypes which map to those long URLs automatically
            var userId = user.FindFirst("oid")?.Value
                         ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var email = user.FindFirst(ClaimTypes.Upn)?.Value
                        ?? user.FindFirst(ClaimTypes.Email)?.Value;

            var firstName = user.FindFirst(ClaimTypes.GivenName)?.Value;
            var lastName = user.FindFirst(ClaimTypes.Surname)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            return new UserClaimsInfo
            {
                UserId = userId,
                Email = email,
                FirstName = firstName ?? "Unknown",
                LastName = lastName ?? "Unknown"
            };
        }
    }
}
