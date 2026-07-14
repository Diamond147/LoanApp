namespace Application.Services.Interfaces.ExternalServices
{
    public interface IPaystackClient
    {
        Task<dynamic> InitializeTransactionAsync(string email, decimal amount, string reference, string? callbackUrl = null);

        Task<dynamic> VerifyTransactionAsync(string reference);
    }
}
