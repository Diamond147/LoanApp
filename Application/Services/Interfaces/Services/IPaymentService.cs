using Domain.DTOs.Payments;
using Domain.Enums;

namespace Application.Services.Interfaces.Services
{
    public interface IPaymentService
    {
        //Task<PaymentResponseDto> InitiatePaymentAsync(InitiatePaymentDto initiatePayment);
        Task<PaymentResponseDto> InitiatePaymentAsync();

        // Verifies a payment with Paystack and updates loan status if successful. Called by webhook when Paystack confirms payment.
        Task<bool> ProcessSuccessfulWebhookAsync(PaystackWebhookData data);

        Task<List<PaymentDto>> GetPaymentsAsync(PaymentStatus? status = null, string? paymentId = null, string? reference = null);

        Task<PaymentDto?> GetPaymentByReferenceAsync(string reference);
        Task<PaymentDto?> GetPaymentByIdAsync(string paymentId);
    }
}
