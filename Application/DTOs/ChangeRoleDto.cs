

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ChangeRoleDto
    {
        [Required(ErrorMessage = "New role is required.")]
        public string NewRole { get; set; } = string.Empty;
    }
}
