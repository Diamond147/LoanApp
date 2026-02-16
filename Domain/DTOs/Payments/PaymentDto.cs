using Domain.Enums;

namespace Domain.DTOs.Payments
{
    public class PaymentDto
    {
        public string? Id { get; set; }
        public string? LoanId { get; set; }
        public string? UserProfileId { get; set; }
        public string? PaystackReference { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public string? PaystackResponse { get; set; }

        //public string? AuthorizationUrl { get; set; }

        //public string? AccessCode { get; set; }
    }
}
