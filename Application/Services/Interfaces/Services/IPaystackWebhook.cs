using Domain.DTOs.Payments;

namespace Application.Services.Interfaces.Services
{
    public interface IPaystackWebhook
    {
        Task<bool> PaystackWebhookAsync(PaystackWebhookDto payload, string requestBody, string signature);
    }
}
