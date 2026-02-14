namespace Domain.Entities
{
    public class AdminDashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalLoans { get; set; }
        public int PendingLoans { get; set; }
        public int ApprovedLoans { get; set; }
        public int RejectedLoans { get; set; }
        public decimal TotalLoanAmount { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public int TotalLoanHistories { get; set; }
    }
}
