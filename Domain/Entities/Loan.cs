
using Domain.Enums;

namespace Domain.Entities
{
    public class Loan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public LoanType LoanType { get; set; }
        public decimal RequestedAmount { get; set; }
        //public decimal? ApprovedAmount { get; set; }
        public LoanStatus Status { get; set; } = LoanStatus.Pending;
        public DateTime RequestedDate { get; set; }
        //public DateTime? ApprovalDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UserProfileId { get; set; } = string.Empty;
        public List<LoanHistory> LoanHistories { get; set; } = new List<LoanHistory>();
    }
}
