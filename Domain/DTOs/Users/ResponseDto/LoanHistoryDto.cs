using Domain.Enums;

namespace Domain.DTOs.Users.ResponseDto
{
    public class LoanHistoryDto
    {
        public string? Id { get; set; }
        public LoanType LoanType { get; set; }
        public string? LoanId { get; set; }
        public decimal RequestedAmount { get; set; }
        public DateTime RequestedDate { get; set; }
        public LoanStatus Status { get; set; }
        public string? UserProfileId { get; set; }
    }
}
