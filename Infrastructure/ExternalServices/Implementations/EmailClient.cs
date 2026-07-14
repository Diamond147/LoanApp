using Azure;
using Azure.Communication.Email;
using Application.Services.Interfaces.ExternalServices;

namespace Infrastructure.ExternalServices
{
    public class AzureEmailClient : IEmailClient
    {
        private readonly EmailClient _emailClient;
        private readonly string _senderEmail;

        public AzureEmailClient(string connectionString, string senderEmail)
        {
            _emailClient = new EmailClient(connectionString);
            _senderEmail = senderEmail;
        }

        public async Task<bool> SendEmailAsync(string emailAddress, string subject, string body, bool isHtml = true)
        {
            try
            {
                var emailContent = new EmailContent(subject)
                {
                    Html = isHtml ? body : null,
                    PlainText = isHtml ? null : body
                };

                var emailMessage = new EmailMessage(_senderEmail, emailAddress, emailContent);

                var emailSendOperation = await _emailClient.SendAsync(
                    WaitUntil.Completed,
                    emailMessage
                );

                Console.WriteLine($"Email sent to {emailAddress}. Status: {emailSendOperation.Value.Status}");
                return emailSendOperation.Value.Status == EmailSendStatus.Succeeded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email error: {ex.Message}");
                return false;
            }
        }
    }
}
