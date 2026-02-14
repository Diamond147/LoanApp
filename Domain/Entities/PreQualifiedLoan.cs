
using Domain.Enums;

namespace Domain.Entities
{
    public class PreQualifiedLoan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public LoanType LoanType { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public string LoanTenure { get; set; } = string.Empty;
    }
}
