using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Users.RequestDto
{
    public class CreateLoanDto
    {
        [Required]
        public LoanType loanType { get; set; }


        [Required(ErrorMessage = "Loan amount is required.")]
        [Range(10000.00, 10000000.00, ErrorMessage = "Loan amount must be between #10,000 and #10,000,000.")]
        [DataType(DataType.Currency, ErrorMessage = "Invalid currency format.")]
        public decimal Amount { get; set; }
    }
}
