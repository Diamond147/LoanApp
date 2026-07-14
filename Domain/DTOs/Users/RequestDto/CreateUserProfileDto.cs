namespace Domain.DTOs.Users.RequestDto
{
    public class CreateUserProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MobileNumber { get; set; }
        public string? Nationality { get; set; }
        //public DateTime SignUpDate { get; set; }
    }
}
