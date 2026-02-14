

using Domain.DTOs.Payments;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Interfaces
{
    public interface IPaymentService
    {
        // Initiates a payment for a user's loan through Paystack.
        Task<PaymentResponseDto> InitiatePaymentAsync(InitiatePaymentDto initiatePayment);

        // Verifies a payment with Paystack and updates loan status if successful. Called by webhook when Paystack confirms payment.
        Task<bool> VerifyPaymentAsync(string reference);

        // Gets all payments made by a specific user.
        Task<List<PaymentDto>> GetPaymentsAsync(PaymentStatus? status = null, string? paymentId = null, string? reference = null);

        // Gets a specific payment by its Paystack reference.

        Task<PaymentDto?> GetPaymentByReferenceAsync(string reference);
        Task<PaymentDto?> GetPaymentByIdAsync(string paymentId);
    }
}
