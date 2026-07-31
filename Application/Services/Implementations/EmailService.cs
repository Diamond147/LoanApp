
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.Entities;
using Application.Services.Interfaces.ExternalServices;
using Domain.DTOs.Emails;

namespace Application.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IEmailClient _emailClient;
        private readonly IEmailRepository _emailRepository;

        public EmailService(IEmailClient emailClient, IEmailRepository emailRepository)
        {
            _emailClient = emailClient;
            _emailRepository=emailRepository;
        }


        public async Task<bool> SendEmailAsync(EmailDto emailDto)
        {
            bool emailSent = false;
            string? errorMessage = null;

            try
            {
                // Attempt to send email through email client
                emailSent = await _emailClient.SendEmailAsync(
                    emailAddress: emailDto.EmailAddress,
                    subject: emailDto.Subject,
                    body: emailDto.Body,
                    isHtml: emailDto.IsHtml);

                // If sending failed, record error
                if (!emailSent)
                {
                    errorMessage = "Email sending failed";
                }
            }
            catch (Exception ex)
            {
                // Record the error for debugging
                emailSent = false;
                errorMessage = ex.Message;

                // Log to console for immediate visibility
                Console.WriteLine($"Error sending email to {emailDto.EmailAddress}: {ex.Message}");
            }

            // Create log entry for this email attempt. An audit trail of all communications
            var emailLog = new EmailLog
            {
                Id = Guid.NewGuid().ToString(),
                UserProfileId = emailDto.UserProfileId,
                EmailAddress = emailDto.EmailAddress,
                Subject = emailDto.Subject,
                Body = emailDto.Body,
                EmailType = emailDto.EmailType,
                IsSent = emailSent,
                SentDate = DateTime.UtcNow,
                ErrorMessage = errorMessage
            };

            // Even if email failed, we log the attempt to db
            await _emailRepository.AddEmailLogAsync(emailLog);

            return emailSent;
        }


        public async Task<bool> SendLoanApprovalEmailAsync(UserProfile user, Loan loan)
        {
            // Create email DTO with all necessary information
            var emailDto = new EmailDto
            {
                UserProfileId = user.Id,
                EmailAddress = user.Email,
                Subject = "Your Loan Application Has Been Approved!",
                Body = $"Dear {user.FirstName} {user.LastName},<br/><br/>" +
                       $"Congratulations! Your loan application for #{loan.RequestedAmount} has been approved.<br/>" +
                       $"Please log in to your account to view details and next steps.<br/><br/>" +
                       $"Best regards,<br/>" +
                       $"Loan Management Team",
                IsHtml = true,
                EmailType = "LoanApproval"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> SendLoanRejectionEmailAsync(UserProfile user, Loan loan)
        {
            var emailDto = new EmailDto
            {
                UserProfileId = user.Id,
                EmailAddress = user.Email,
                Subject = "Your Loan Application Has Been Rejected",
                Body = $"Dear {user.FirstName} {user.LastName},<br/><br/>" +
                       $"We regret to inform you that your loan application for #{loan.RequestedAmount} has been rejected.<br/>" +
                       $"Please log in to your account for more information or to apply again in the future.<br/><br/>" +
                       $"Best regards,<br/>" +
                       $"Loan Management Team",
                IsHtml = true,
                EmailType = "LoanRejection"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> SendPaymentConfirmationEmailAsync(UserProfile user, Loan loan, Payment payment)
        {
            var emailDto = new EmailDto
            {
                UserProfileId = user.Id,
                EmailAddress = user.Email,
                Subject = "Payment Confirmation",
                Body = $"Dear {user.FirstName} {user.LastName},<br/><br/>" +
                       $"We have received your payment of #{payment.Amount} for your loan of #{loan.RequestedAmount}.<br/>" +
                       $"Your remaining balance is #{loan.RequestedAmount - payment.Amount}.<br/><br/>" +
                       $"Thank you for your payment!<br/>" +
                       $"Best regards,<br/>" +
                       $"Loan Management Team",
                IsHtml = true,
                EmailType = "PaymentConfirmation"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> SendPaymentFailureEmailAsync(UserProfile user, Loan loan, Payment payment)
        {
            var emailDto = new EmailDto
            {
                UserProfileId = user.Id,
                EmailAddress = user.Email,
                Subject = "Payment Failure Notification",
                Body = $"Dear {user.FirstName} {user.LastName},<br/><br/>" +
                       $"We were unable to process your payment of #{payment.Amount} for your loan of #{loan.RequestedAmount}.<br/>" +
                       $"Please log in to your account to update your payment information or try again.<br/><br/>" +
                       $"Best regards,<br/>" +
                       $"Loan Management Team",
                IsHtml = true,
                EmailType = "PaymentFailure"
            };

            return await SendEmailAsync(emailDto);
        }
    }
}
