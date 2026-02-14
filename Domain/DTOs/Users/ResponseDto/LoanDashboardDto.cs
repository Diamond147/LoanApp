namespace Domain.DTOs.Users.ResponseDto
{
    public class LoanDashboardDto
    {
        public UserProfileDto? User { get; set; }
        public List<LoanDto> Loans { get; set; } = new();
        public List<LoanHistoryDto> LoanHistory { get; set; } = new();
    }
}
