
using Domain.Enums;

namespace Domain.Entities
{
    public class PreQualifiedLoan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public LoanType? LoanType { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int LoanTenureInMonths { get; set; }

        // Annual interest rate as decimal (e.g. 0.5 = 50%)
        public decimal InterestRate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
