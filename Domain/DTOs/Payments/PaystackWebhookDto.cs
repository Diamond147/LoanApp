
using System.Text.Json.Serialization;

namespace Domain.DTOs.Payments
{
    // DTO for data received from Paystack webhook.
    // When a payment is completed, Paystack sends a POST request to our webhook URL
    // with this data structure. We use it to verify and confirm the payment.
    public class PaystackWebhookDto
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public PaystackWebhookData Data { get; set; } = new();
    }

    public class PaystackWebhookData
    {
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        //[JsonPropertyName("customer")]
        //public PaystackCustomer Customer { get; set; } = new();

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    //public class PaystackCustomer
    //{
    //    [JsonPropertyName("email")]
    //    public string Email { get; set; } = string.Empty;
    //}
}
