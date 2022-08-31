using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;

namespace NetSuiteRestApiOAuth2
{
    internal class NetSuiteTokenClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _accountId;
        private readonly string _consumerKey;
        private readonly string _certificateId;
        private readonly string _privateKeyPath;

        private AccessToken? _accessToken;

        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

        public NetSuiteTokenClient(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            var cfg = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _accountId = cfg.GetValue<string>("NetSuite:AccountId");
            _consumerKey = cfg.GetValue<string>("NetSuite:ConsumerKey");
            _certificateId = cfg.GetValue<string>("NetSuite:CertificateId");
            _privateKeyPath = cfg.GetValue<string>("NetSuite:PrivateKeyPath");
            _httpClient.BaseAddress = new Uri($"https://{_accountId}.suitetalk.api.netsuite.com");
        }

        public async Task<AccessToken> GetAccessToken(CancellationToken cancellationToken)
        {
            try
            {
                await _semaphoreSlim.WaitAsync(cancellationToken);

                if (_accessToken is null || _accessToken.IsAboutToExpire)
                {
                    _accessToken = await FetchToken();
                }

                return _accessToken;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task<AccessToken> FetchToken()
        {
            string privKey = File.ReadAllText(_privateKeyPath);
            privKey = privKey.Replace("-----BEGIN PRIVATE KEY-----", "");
            privKey = privKey.Replace("-----END PRIVATE KEY-----", "");

            byte[] privateKeyRaw = Convert.FromBase64String(privKey);
            RSACryptoServiceProvider provider = new();
            provider.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(privateKeyRaw), out _);
            RsaSecurityKey rsaSecurityKey = new(provider);

            var signingCreds = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
            };
            signingCreds.Key.KeyId = _certificateId;

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _consumerKey,
                Audience = $"https://{_accountId}.suitetalk.api.netsuite.com/services/rest/auth/oauth2/v1/token",
                IssuedAt = DateTime.UtcNow,
                Claims = new Dictionary<string, object> { { "scope", new[] { "rest_webservices", "restlets" } } },
                SigningCredentials = signingCreds
            };

            var jwt = tokenHandler.CreateToken(tokenDescriptor);
            var clientAssertion = tokenHandler.WriteToken(jwt);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/services/rest/auth/oauth2/v1/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                    { "client_assertion", clientAssertion }
                })
            };
            using var httpResponse = await _httpClient.SendAsync(httpRequest);
            httpResponse.EnsureSuccessStatusCode();

            await using var responseContent = await httpResponse.Content.ReadAsStreamAsync();

            var accessToken = await JsonSerializer.DeserializeAsync<AccessToken>(responseContent);

            return accessToken ?? throw new Exception("Failed to deserialize the access token!");
        }
    }
}
