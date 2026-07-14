using Application.Services.Interfaces.ExternalServices;

namespace Infrastructure.ExternalServices
{
    // A simple no-op email client for development and testing. Logs to console and returns true.
    public class NoOpEmailClient : IEmailClient
    {
        public Task<bool> SendEmailAsync(string emailAddress, string subject, string body, bool isHtml = true)
        {
            Console.WriteLine($"[NoOpEmailClient] Simulated send to {emailAddress} - Subject: {subject}");
            return Task.FromResult(true);
        }
    }
}
