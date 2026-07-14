using Domain.Enums;

namespace Domain.DTOs.Users.RequestDto
{
    public class CreateLoanDto
    {
        public LoanType loanType { get; set; }
        public decimal Amount { get; set; }
    }
}
