namespace Domain.DTOs.Users.RequestDto
{
    public class CreatePreQualifiedLoanDto
    {
        //public LoanType LoanType { get; set; }

        public decimal MinAmount { get; set; }

        public decimal MaxAmount { get; set; }
        public string LoanTenure { get; set; } = string.Empty;
    }
}
