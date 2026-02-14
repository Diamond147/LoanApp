namespace Domain.DTOs.Users.ResponseDto
{
    public class ContinuationResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public string? ContinuationToken { get; set; }
        public bool HasMore { get; set; }
    }
}
