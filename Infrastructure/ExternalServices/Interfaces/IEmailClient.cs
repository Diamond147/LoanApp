

namespace Infrastructure.ExternalServices.Interfaces
{
    public interface IEmailClient
    {
        Task<bool> SendEmailAsync(string emailAddress, string subject, string body, bool isHtml = true);
    }
}
