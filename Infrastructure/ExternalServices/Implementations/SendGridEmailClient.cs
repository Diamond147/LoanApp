using Application.Services.Interfaces.ExternalServices;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Infrastructure.ExternalServices.Implementations
{
    public class SendGridEmailClient : IEmailClient
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public SendGridEmailClient(string apiKey, string senderEmail, string senderName = "Loan App")
        {
            _sendGridClient = new SendGridClient(apiKey);
            _senderEmail = senderEmail;
            _senderName = senderName;
        }


        public async Task<bool> SendEmailAsync(string emailAddress, string subject, string body, bool isHtml = true)
        {
            try
            {
                var from = new EmailAddress(_senderEmail, _senderName);
                var to = new EmailAddress(emailAddress);

                var msg = MailHelper.CreateSingleEmail(
                    from,
                    to,
                    subject,
                    plainTextContent: isHtml ? null : body,
                    htmlContent: isHtml ? body : null
                );

                var response = await _sendGridClient.SendEmailAsync(msg);

                // SendGrid returns 200 OK or 202 Accepted on success
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Email sent to {emailAddress} via SendGrid. Status: {response.StatusCode}");
                    return true;
                }

                var errorBody = await response.Body.ReadAsStringAsync();
                Console.WriteLine($"SendGrid error ({response.StatusCode}): {errorBody}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendGrid Exception: {ex.Message}");
                return false;
            }
        }
    }
}