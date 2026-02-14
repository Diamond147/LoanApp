namespace Domain.DTOs.Users.ResponseDto
{
    public class UserProfileDto
    {
        public string? Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime SignUpDate { get; set; }
        public string? Gender { get; set; }
        public string? MobileNumber { get; set; } 
        public string? Nationality { get; set; }
        public DateOnly? DateOfBirth { get; set; }

    }
}
