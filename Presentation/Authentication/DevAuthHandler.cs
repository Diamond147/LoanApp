//using Microsoft.AspNetCore.Authentication;
//using System.Security.Claims;
//using System.Text.Encodings.Web;
//using Microsoft.Extensions.Options;

//namespace Presentation.Authentication
//{
//    // A tiny development authentication handler that creates an authenticated principal from a header.
//    // Header: X-Dev-User: {username}; optionally set X-Dev-Roles: Admin,User
//    public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
//    {
//        public DevAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
//            : base(options, logger, encoder, clock)
//        {
//        }

//        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
//        {
//            // Only apply when Development environment is set in Program.cs
//            var headerUser = Request.Headers["X-Dev-User"].ToString();
//            var headerRoles = Request.Headers["X-Dev-Roles"].ToString();

//            if (string.IsNullOrEmpty(headerUser))
//            {
//                // default dev user
//                headerUser = "devadmin@local";
//            }

//            var claims = new List<Claim>
//            {
//                new Claim(ClaimTypes.Name, headerUser),
//                new Claim(ClaimTypes.NameIdentifier, headerUser)
//            };

//            if (!string.IsNullOrEmpty(headerRoles))
//            {
//                var roles = headerRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
//                foreach (var role in roles)
//                {
//                    claims.Add(new Claim(ClaimTypes.Role, role));
//                }
//            }
//            else
//            {
//                // default to Admin role for convenience
//                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
//            }

//            var identity = new ClaimsIdentity(claims, Scheme.Name);
//            var principal = new ClaimsPrincipal(identity);
//            var ticket = new AuthenticationTicket(principal, Scheme.Name);

//            return Task.FromResult(AuthenticateResult.Success(ticket));
//        }
//    }
//}
