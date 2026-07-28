using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Users.RequestDto
{
    public class CreatePreQualifiedLoanDto
    {
        [Required]
        public LoanType LoanType { get; set; }


        [Required(ErrorMessage = "Minimum amount is required.")]
        [Range(10000.00, double.MaxValue, ErrorMessage = "Minimum amount is #10,000.")]
        [DataType(DataType.Currency, ErrorMessage = "Invalid currency format.")]
        public decimal MinAmount { get; set; }


        [Required(ErrorMessage = "Maximum amount is required.")]
        [Range(double.MinValue, 999999.00, ErrorMessage = "Maximum amount is #999,999.")]
        [DataType(DataType.Currency, ErrorMessage = "Invalid currency format.")]
        public decimal MaxAmount { get; set; }

        
        [Required(ErrorMessage = "Loan tenure is required.")]
        [Range(3, 72, ErrorMessage = "Loan tenure must be between 3 and 72 months.")]
        [DataType(DataType.Text, ErrorMessage = "Invalid loan tenure format.")]
        public int LoanTenureInMonths { get; set; }
    }
}
