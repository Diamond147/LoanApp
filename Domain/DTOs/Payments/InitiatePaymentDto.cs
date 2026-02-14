
using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Payments
{
    public class InitiatePaymentDto
    {
        // Must be an approved loan belonging to the authenticated user
        [Required(ErrorMessage = "Loan ID is required")]
        public string LoanId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }
}
