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
        [DataType(DataType.Currency, ErrorMessage = "Invalid currency format.")]
        public decimal RequestedAmount { get; set; }
    }
}
