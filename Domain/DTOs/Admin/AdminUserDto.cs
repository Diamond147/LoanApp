namespace Domain.DTOs.Admin
{
    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? MobileNumber { get; set; }
        public DateTime? SignUpDate { get; set; }
        public string? Nationality { get; set; }
    }
}
