
using Domain.DTOs.Emails;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IEmailService
    {
        // Sends an email and logs the attempt.
        Task<bool> SendEmailAsync(EmailDto emailDto);

        // Sends loan approval notification to user.
        Task<bool> SendLoanApprovalEmailAsync(UserProfile user, Loan loan);

        // Sends loan rejection notification to user.
        Task<bool> SendLoanRejectionEmailAsync(UserProfile user, Loan loan);

        // Sends payment confirmation email to user.
        Task<bool> SendPaymentConfirmationEmailAsync(UserProfile user, Loan loan, Payment payment);

        // Sends payment failure notification to user.
        Task<bool> SendPaymentFailureEmailAsync(UserProfile user, Loan loan, Payment payment);
    }
}
