
using Domain.Enums;

namespace Domain.Entities
{
    public class Loan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public LoanType? LoanType { get; set; }
        public decimal RequestedAmount { get; set; }

        // Annual interest rate as decimal (e.g. 0.5 for 50%)
        public decimal? InterestRate { get; set; }

        // Accrued interest not yet paid
        public decimal AccruedInterest { get; set; }

        // The date up to which interest has been accrued
        public DateTime? LastInterestAccrualDate { get; set; }

        public LoanStatus Status { get; set; } = LoanStatus.Pending;
        public DateTime? ApprovalDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Principal remaining (starts as RequestedAmount when approved)
        public decimal PrincipalBalance { get; set; }
        public decimal OutstandingAmount => PrincipalBalance + AccruedInterest; // computed, always current
        public DateTime RequestedDate { get; set; }
        
        public string UserProfileId { get; set; } = string.Empty;
        public List<LoanHistory> LoanHistories { get; set; } = new List<LoanHistory>();

    }
}
