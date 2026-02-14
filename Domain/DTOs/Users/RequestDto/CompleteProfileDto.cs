using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Users.RequestDto
{
    // To complete profile after sign-in
    public class CompleteProfileDto
    {
        public string Gender { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        [Phone]
        [StringLength(11, ErrorMessage = "Mobile number must be 11 digits.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Mobile number must contain only digits.")]
        public string MobileNumber { get; set; } = string.Empty;

        public string Nationality { get; set; } = string.Empty;
    }
}
