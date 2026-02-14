
namespace Domain.Entities
{
    public class ContinuationTokenData
    {
        public int Skip { get; set; }
        public DateTime? LastSortValue { get; set; } // For future optimization
    }
}
