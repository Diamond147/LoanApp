

using Domain.DTOs.Payments;
using Domain.Enums;

namespace Application.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> InitiatePaymentAsync(InitiatePaymentDto initiatePayment);

        // Verifies a payment with Paystack and updates loan status if successful. Called by webhook when Paystack confirms payment.
        Task<bool> VerifyPaymentAsync(string reference);

        Task<List<PaymentDto>> GetPaymentsAsync(PaymentStatus? status = null, string? paymentId = null, string? reference = null);

        Task<PaymentDto?> GetPaymentByReferenceAsync(string reference);
        Task<PaymentDto?> GetPaymentByIdAsync(string paymentId);
    }
}
