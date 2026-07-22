using Domain.Enums;

namespace Domain.DTOs.Users.ResponseDto
{
    public class LoanDto
    {
        public string? Id { get; set; }
        public LoanType LoanType { get; set; }
        public decimal RequestedAmount { get; set; }
        //public decimal AccruedInterest { get; set; }
        public LoanStatus Status { get; set; }
        public DateTime RequestedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UserProfileId { get; set; }
    }
}
