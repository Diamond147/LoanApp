namespace Domain.DTOs.Admin
{
    public class AdminUserDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public DateTime? SignUpDate { get; set; }

        // Loans List
        public List<AdminLoanDto> Loans { get; set; } = new();

        // Loan Histories List
        public List<AdminLoanHistoryDto> LoanHistories { get; set; } = new();
    }
}
