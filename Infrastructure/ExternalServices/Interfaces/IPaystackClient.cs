

namespace Infrastructure.ExternalServices.Interfaces
{
    public interface IPaystackClient
    {
        // Initializes a payment transaction with Paystack.
        Task<dynamic> InitializeTransactionAsync(string email, decimal amount, string reference, string? callbackUrl = null);

        // Verifies a payment transaction with Paystack.
        Task<dynamic> VerifyTransactionAsync(string reference);
    }
}
