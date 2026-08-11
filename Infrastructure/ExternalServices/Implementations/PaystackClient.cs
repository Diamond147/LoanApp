using Application.Services.Interfaces.ExternalServices;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Infrastructure.ExternalServices.Implementations
{
    // Handles all HTTP communication with Paystack's REST API.
    // Uses HttpClient to make requests and parses JSON responses.
    public class PaystackClient : IPaystackClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private const string BaseUrl = "https://api.paystack.co";

        public PaystackClient(HttpClient httpClient, string secretKey)
        {
            _httpClient = httpClient;
            _secretKey = secretKey;
            _httpClient.BaseAddress = new Uri(BaseUrl);

            // Add Authorization header with secret key
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _secretKey);

            // Tell Paystack we want JSON responses
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }


        // API Endpoint: POST https://api.paystack.co/transaction/initialize
        public async Task<dynamic> InitializeTransactionAsync(
            string email,
            decimal amount,
            string reference,
            string? callbackUrl = null)
        {
            var payload = new
            {
                email = email,
                amount = (amount * 100).ToString("F0"), 
                reference = reference,
                callback_url = callbackUrl // Optional: where to redirect after payment
            };

            // Serialize payload to JSON
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // Send POST request to Paystack
            var response = await _httpClient.PostAsync("/transaction/initialize", content);

            // Ensure request was successful (status code 2xx) and throws HttpRequestException if status is 4xx or 5xx
            response.EnsureSuccessStatusCode();

            // Read response body as string
            var responseBody = await response.Content.ReadAsStringAsync();

            // Parse JSON response into dynamic object
            var result = JsonSerializer.Deserialize<dynamic>(responseBody);

            return result ?? throw new InvalidOperationException("Failed to deserialize Paystack response");
        }

        public async Task<dynamic> VerifyTransactionAsync(string reference)
        {
            // Send GET request to verify endpoint. No request body needed - reference is in URL
            var response = await _httpClient.GetAsync($"/transaction/verify/{reference}");

            // Ensure request was successful
            response.EnsureSuccessStatusCode();

            // Read and parse response
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<dynamic>(responseBody);

            return result ?? throw new InvalidOperationException("Failed to deserialize Paystack response");
        }
    }
}
