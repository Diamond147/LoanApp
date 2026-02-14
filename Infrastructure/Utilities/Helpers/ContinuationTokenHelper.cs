using Domain.Entities;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.Utilities.Helpers
{
    public static class ContinuationTokenHelper
    {
        public static string Encode(int skip, DateTime? lastSortValue = null)
        {
            var tokenData = new ContinuationTokenData
            {
                Skip = skip,
                LastSortValue = lastSortValue
            };

            var json = JsonSerializer.Serialize(tokenData);
            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }

        public static ContinuationTokenData? Decode(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var bytes = Convert.FromBase64String(token);
                var json = Encoding.UTF8.GetString(bytes);
                return JsonSerializer.Deserialize<ContinuationTokenData>(json);
            }
            catch
            {
                return null; // Invalid token
            }
        }
    }
}
