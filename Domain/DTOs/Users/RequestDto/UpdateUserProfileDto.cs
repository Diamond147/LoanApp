using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Users.RequestDto
{
    //Update = PUT (Full Replacement)
    public class UpdateUserProfileDto
    {
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }

        [Phone]
        [StringLength(11, ErrorMessage = "Mobile number must be 11 digits.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Mobile number must contain only digits.")]
        public string? MobileNumber { get; set; }

        public string? Nationality { get; set; }
    }
}
