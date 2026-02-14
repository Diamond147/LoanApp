
namespace Domain.DTOs.Emails
{
    // DTO for sending emails through the email service.
    public class EmailDto
    {
        public string EmailAddress { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        // True = HTML email (with formatting), False = Plain text email
        public bool IsHtml { get; set; } = true;

        // "LoanApproval", "PaymentConfirmation", "LoanRejection". Used for logging and tracking
        public string EmailType { get; set; } = string.Empty;
        public string UserProfileId { get; set; } = string.Empty;
    }
}
