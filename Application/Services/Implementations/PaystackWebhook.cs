using Domain.DTOs.Payments;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Application.Services.Interfaces.Services;

namespace Application.Services.Implementations
{
    public class PaystackWebhook : IPaystackWebhook
    {
        private readonly IConfiguration _configuration;
        private readonly IPaymentService _paymentService;

        public PaystackWebhook(IConfiguration configuration, IPaymentService paymentService)
        {
            _configuration = configuration;
            _paymentService=paymentService;
        }


        public async Task<bool> PaystackWebhookAsync( PaystackWebhookDto payload, string requestBody, string signature)
        {
            Console.WriteLine($"Request body length: {requestBody.Length}");
            Console.WriteLine($"Request body: {requestBody}");

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                Console.WriteLine("EMPTY BODY RECEIVED - Check if Paystack sent data.");
                return true; // Still return 200 to stop retries
            }

            // Get webhook secret from configuration
            var secret = _configuration["Paystack:WebhookSecret"];

            // FAIL-CLOSED SECURITY: Reject if secret is missing or signature verification fails
            if (string.IsNullOrEmpty(secret) || !VerifySignature(requestBody, signature, secret))
            {
                Console.WriteLine("INVALID OR UNCONFIGURED SIGNATURE DETECTED.");
                return false;
            }

            // Parse and process webhook data
            try
            {
                if (payload.Event == "charge.success")
                {
                    Console.WriteLine($"Processing payment for reference: {payload.Data.Reference}");

                    // Process directly using authenticated payload to avoid redundant network calls
                    var result = await _paymentService.ProcessSuccessfulWebhookAsync(payload.Data);
                    if (result)
                        Console.WriteLine("Payment processed successfully");
                    else
                        Console.WriteLine("Failed to process payment");
                }
                else
                {
                    // Other event type - we're not interested
                    Console.WriteLine($"Ignoring event type: {payload.Event}");
                }

                return true;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                return true; // Return 200 so Paystack doesn't keep hitting your error
            }
        }

        private bool VerifySignature(string payload, string signature, string secret)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature)) return false;

            // NOTE: HMAC algorithm works with byte arrays
            var secretBytes = Encoding.UTF8.GetBytes(secret);

            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            // Create HMAC SHA-512 hasher with secret key
            using var hmac = new HMACSHA512(secretBytes);

            // This creates a byte array containing the hash
            var hashBytes = hmac.ComputeHash(payloadBytes);

            // Convert hash bytes to hexadecimal string bcos Paystack sends signature as hex string
            var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            // Compare computed signature with signature from Paystack
            return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }
    }
}
