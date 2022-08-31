using System.Text.Json.Serialization;

namespace NetSuiteRestApiOAuth2
{
    internal class AccessToken
    {
        private static readonly TimeSpan _expirеThreshold = new(0, 5, 0);
        public AccessToken(string tokenType, string token, int expiresIn)
        {
            TokenType = tokenType;
            Token = token;
            ExpiresIn = expiresIn;
            ExpiresAt = DateTime.UtcNow.AddSeconds(ExpiresIn);
        }

        [JsonPropertyName("token_type")]
        public string TokenType { get; }

        [JsonPropertyName("access_token")]
        public string Token { get; }

        [JsonPropertyName("expires_in")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int ExpiresIn { get; }
        public DateTime ExpiresAt { get; }

        public bool IsAboutToExpire => (ExpiresAt - DateTime.UtcNow).TotalSeconds <= _expirеThreshold.TotalSeconds;
    }
}
