using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Users.RequestDto
{
    public class CreateLoanDto
    {
        [Required(ErrorMessage = "The loan type is required.")]
        [EnumDataType(typeof(LoanType), ErrorMessage = "Invalid loan type.")]
        public LoanType? loanType { get; set; }


        [Required(ErrorMessage = "Loan amount is required.")]
        //[Range(10000.00, 999999.00, ErrorMessage = "Loan amount must be between #10,000 and #999,999.")]
        [DataType(DataType.Currency, ErrorMessage = "Invalid currency format.")]
        public decimal Amount { get; set; }
    }
}
