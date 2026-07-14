namespace Application.Services.Interfaces.ExternalServices
{
    public interface IEmailClient
    {
        Task<bool> SendEmailAsync(string emailAddress, string subject, string body, bool isHtml = true);
    }
}
