using Domain.Entities;
using System.Security.Claims;


namespace Application.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static UserClaimsInfo GetUserInfo(this ClaimsPrincipal user)
        {
            if (user == null)
                throw new UnauthorizedAccessException("User is not authenticated");

            // Prefer checking for the presence of claims rather than relying on Identity.IsAuthenticated
            // which can be false in unit tests when AuthenticationType is null. If there are no claims,
            // treat the principal as unauthenticated.
            if (!user.Claims.Any())
                throw new UnauthorizedAccessException("User is not authenticated");

            // Attempt multiple common claim types to be more robust in different auth setups
            var userId = user.FindFirst("oid")?.Value
                         ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value
                         ?? user.FindFirst("id")?.Value;

            var email = user.FindFirst(ClaimTypes.Upn)?.Value
                        ?? user.FindFirst(ClaimTypes.Email)?.Value
                        ?? user.FindFirst("email")?.Value;

            var firstName = user.FindFirst(ClaimTypes.GivenName)?.Value;
            var lastName = user.FindFirst(ClaimTypes.Surname)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                // Keep the same exception type for callers that expect Unauthorized
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
