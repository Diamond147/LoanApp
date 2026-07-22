using Domain.Enums;

namespace Domain.DTOs.Users.ResponseDto
{
    public class PreQualifiedLoanDto
    {
        public string? Id { get; set; }
        public LoanType LoanType { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int LoanTenureInMonths { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
