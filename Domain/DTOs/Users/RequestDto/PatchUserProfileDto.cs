using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Users.RequestDto
{
    //PATCH(Partial Modification)
    public class PatchUserProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }

        [Phone]
        [StringLength(11, ErrorMessage = "Mobile number must be 11 digits.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Mobile number must contain only digits.")]
        public string? MobileNumber { get; set; }
        public string? Nationality { get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }
}
