using Domain.Enums;

namespace Domain.DTOs.Admin
{
    public class AdminLoanDto
    {
        public string Id { get; set; } = string.Empty;
        public LoanType LoanType { get; set; }
        public decimal Amount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public LoanStatus Status { get; set; }
        public DateTime? RequestedDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string UserProfileId { get; set; } = string.Empty;
        public string? UserName { get; set; }
    }
}
