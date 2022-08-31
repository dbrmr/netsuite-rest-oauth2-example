using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace NetSuiteRestApiOAuth2
{
    internal class NetSuiteApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _accountId;

        public NetSuiteApiClient(IConfiguration configuration, HttpClient httpClient)
        {
            var cfg = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _accountId = cfg.GetValue<string>("NetSuite:AccountId");
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri($"https://{_accountId}.suitetalk.api.netsuite.com");
            _httpClient.DefaultRequestHeaders.Add("Prefer", "respond-async");
        }

        public async Task<Customer> GetCustomer(string id)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/services/rest/record/v1/customer/{id}");
            using var httpResponse = await _httpClient.SendAsync(httpRequest);
            httpResponse.EnsureSuccessStatusCode();

            await using var responseContent = await httpResponse.Content.ReadAsStreamAsync();

            var customer = await JsonSerializer.DeserializeAsync<Customer>(responseContent);

            return customer ?? throw new Exception("Failed to deserialize Customer!");
        }
    }
}
