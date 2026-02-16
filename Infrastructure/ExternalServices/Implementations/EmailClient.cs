
//using System.Net;
//using System.Net.Mail;
//using Infrastructure.ExternalServices.Interfaces;

//namespace Infrastructure.ExternalServices.Implementations
//{
//    public class EmailClient : IEmailClient
//    {
//        private readonly string _smtpServer;
//        private readonly int _smtpPort;
//        private readonly string _senderEmail;
//        private readonly string _senderPassword;
//        private readonly string _senderName;

//        // Constructor that loads SMTP configuration from appsettings.json. Configuration is injected via dependency injection.
//        public EmailClient(string smtpServer, int smtpPort, string senderEmail, string senderPassword, string senderName)
//        {
//            _smtpServer = smtpServer;
//            _smtpPort = smtpPort;
//            _senderEmail = senderEmail;
//            _senderPassword = senderPassword;
//            _senderName = senderName;
//        }

//        public async Task<bool> SendEmailAsync(string emailAddress, string subject, string body, bool isHtml = true)
//        {
//            try
//            {
//                Console.WriteLine($"To: {emailAddress}");
//                Console.WriteLine($"Subject: {subject}");
//                using var message = new MailMessage();

//                // Appears as "Loan Management System <youremail@gmail.com>"
//                message.From = new MailAddress(_senderEmail, _senderName);

//                // Set recipient
//                message.To.Add(new MailAddress(emailAddress));
//                message.Subject = subject;
//                message.Body = body;
//                message.IsBodyHtml = isHtml;

//                // SmtpClient handles the actual sending through SMTP protocol
//                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort);

//                // Enable TLS encryption for security. TLS encrypts the connection between your app and Gmail
//                smtpClient.EnableSsl = true;

//                // Set authentication credentials using the app password generated
//                smtpClient.Credentials = new NetworkCredential(_senderEmail, _senderPassword);

//                //How long to wait for Gmail to respond before giving up
//                smtpClient.Timeout = 30000; // 30 seconds

//                // This connects to Gmail's SMTP server, authenticates, and sends
//                await smtpClient.SendMailAsync(message);
//                Console.WriteLine($"=== EMAIL SENT SUCCESSFULLY ===");

//                return true;
//            }
//            catch (SmtpException smtpEx)
//            {
//                // SMTP-specific errors (authentication, connection, etc.)
//                Console.WriteLine($"SMTP Error sending email to {emailAddress}: {smtpEx.Message}");

//                // Log specific error types for debugging
//                if (smtpEx.StatusCode == SmtpStatusCode.MailboxUnavailable)
//                {
//                    Console.WriteLine("Recipient email address is invalid or doesn't exist");
//                }
//                else if (smtpEx.InnerException != null)
//                {
//                    Console.WriteLine($"Inner exception: {smtpEx.InnerException.Message}");
//                }

//                return false;
//            }
//            catch (Exception ex)
//            {
//                // General errors (network issues, invalid email format, etc.)
//                Console.WriteLine($"General error sending email to {emailAddress}: {ex.Message}");
//                return false;
//            }
//        }
//    }
//}



using Azure;
using Azure.Communication.Email;
using Infrastructure.ExternalServices.Interfaces;
using System.Net.Mail;

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
