
namespace Domain.Entities
{
    public class UserProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Gender { get; set; } 
        public DateOnly? DateOfBirth { get; set; }
        public string? MobileNumber { get; set; } 
        public string? Nationality { get; set; }
        public DateTime SignUpDate { get; set; }
        public List<Loan> Loans { get; set; } = new List<Loan>();
    }
}
