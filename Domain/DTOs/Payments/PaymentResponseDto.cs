
namespace Domain.DTOs.Payments
{
    public class PaymentResponseDto
    {
        /// Redirect user to this URL after initiating payment, where user completes payment on Paystack
        public string AuthorizationUrl { get; set; } = string.Empty;
        
        // Used to track and verify this specific payment
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string LoanId { get; set; } = string.Empty;
    }
}
