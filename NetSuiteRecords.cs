using System.Text.Json.Serialization;

namespace NetSuiteRestApiOAuth2
{

    public class Customer
    {
        public Customer(string id, string companyName)
        {
            Id = id;
            CompanyName = companyName;
        }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; }
    }

}
