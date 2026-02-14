

namespace Infrastructure.ExternalServices.Interfaces
{
    // Interface for email client (SMTP or email service provider).
    public interface IEmailClient
    {
        Task<bool> SendEmailAsync(string emailAddress, string subject, string body, bool isHtml = true);
    }
}
