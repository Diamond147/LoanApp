

namespace Application.Services.Interfaces.ExternalServices
{
    public interface ITokenService
    {
        string GenerateAccessToken(string userId, string email, IEnumerable<string> roles);
    }
}
