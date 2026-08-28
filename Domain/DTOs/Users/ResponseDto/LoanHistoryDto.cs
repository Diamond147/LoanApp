using Domain.Enums;

namespace Domain.DTOs.Users.ResponseDto
{
    public class LoanHistoryDto
    {
        public string? Id { get; set; }
        public string? LoanId { get; set; }
        public LoanType? LoanType { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal? OutstandingAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? AccruedInterest { get; set; }
        public DateTime RequestedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public LoanStatus Status { get; set; }
        public string? UserProfileId { get; set; }
    }
}
