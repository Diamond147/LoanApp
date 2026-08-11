using Microsoft.Extensions.Diagnostics.HealthChecks;


namespace Infrastructure.HealthChecks
{
    public class PaystackHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;

        public PaystackHealthCheck(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Ping Paystack API ping/status endpoint
                var response = await _httpClient.GetAsync("https://api.paystack.co/", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Paystack API is reachable.");
                }

                return HealthCheckResult.Degraded($"Paystack API returned status code {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Paystack API is unreachable.", ex);
            }
        }
    }
}
