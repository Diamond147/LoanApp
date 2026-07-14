using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        //Stores initial payment info before redirecting to Paystack.
        Task<Payment> CreatePaymentAsync(Payment payment);

        // used to find payment when webhook arrives and verify payment
        Task<Payment?> GetPaymentByReferenceAsync(string reference);
        Task<Payment?> GetPaymentByIdAsync(string paymentId);
        Task<List<Payment>> GetPaymentsByLoanIdAsync(string loanId);
        Task<List<Payment>> GetPaymentsAsync(PaymentStatus? status = null, string? paymentId = null, string? reference = null);
        Task<Payment> UpdatePaymentAsync(Payment payment);
    }
}
