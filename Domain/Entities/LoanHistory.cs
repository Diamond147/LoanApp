

using Domain.Enums;

namespace Domain.Entities
{
    public class LoanHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string LoanId { get; set; } = string.Empty;
        public LoanType LoanType { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public DateTime RequestedDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public LoanStatus Status { get; set; }
        public string UserProfileId { get; set; } = string.Empty;
        public Loan? Loan { get; set; }
    }
}
