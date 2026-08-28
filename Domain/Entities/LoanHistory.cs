

using Domain.Enums;

namespace Domain.Entities
{
    public class LoanHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string LoanId { get; set; } = string.Empty;
        public LoanType? LoanType { get; set; }
        public decimal RequestedAmount { get; set; }


        // Snapshot of interest rate at time of history
        public decimal? InterestRate { get; set; }

        // Snapshot of accrued interest at time of history
        public decimal AccruedInterest { get; set; }

        public DateTime RequestedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public LoanStatus Status { get; set; }
        public string UserProfileId { get; set; } = string.Empty;
        public Loan? Loan { get; set; }

        // Snapshot of outstanding principal at time of history
        public decimal PrincipalBalance { get; set; }
        public decimal OutstandingAmount => PrincipalBalance + AccruedInterest; // computed, always current
    }
}
