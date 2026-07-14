using Application.Services.Interfaces.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.ExternalServices.Implementations
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(string userId, string email, IEnumerable<string> roles)
        {
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var durationMinutes = double.Parse(_configuration["Jwt:DurationInMinutes"] ?? "15");
            var privateKeyBase64 = _configuration["Jwt:RsaPrivateKey"]?.Trim()
                ?? throw new InvalidOperationException("RSA Private key is missing from config.");

            var rsa = RSA.Create();
            // The standard reader will now parse the long key perfectly!
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);


            // Define the Identity claims inside the payload
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            // Add application authorization roles dynamically
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Create Asymmetric Signing Credentials using RS256
            var signingCredentials = new SigningCredentials(
                new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256
            );

            // Construct token description envelope
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(durationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
