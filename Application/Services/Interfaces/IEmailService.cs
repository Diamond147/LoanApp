
using Domain.DTOs.Emails;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailDto emailDto);

        Task<bool> SendLoanApprovalEmailAsync(UserProfile user, Loan loan);

        Task<bool> SendLoanRejectionEmailAsync(UserProfile user, Loan loan);

        Task<bool> SendPaymentConfirmationEmailAsync(UserProfile user, Loan loan, Payment payment);

        Task<bool> SendPaymentFailureEmailAsync(UserProfile user, Loan loan, Payment payment);
    }
}
