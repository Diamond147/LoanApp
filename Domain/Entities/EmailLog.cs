
namespace Domain.Entities
{
    // T0 track sent emails
    public class EmailLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? UserProfileId { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        // "LoanApproval", "PaymentConfirmation", "LoanRejection"
        public string EmailType { get; set; } = string.Empty;
        public bool IsSent { get; set; }
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public string? ErrorMessage { get; set; }
    }
}
