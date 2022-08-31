using System.Net.Http.Headers;

namespace NetSuiteRestApiOAuth2
{
    internal class NetSuiteAuthHandler : DelegatingHandler
    {
        private readonly NetSuiteTokenClient _tokenClient;

        public NetSuiteAuthHandler(NetSuiteTokenClient tokenClient)
        {
            _tokenClient = tokenClient;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenClient.GetAccessToken(cancellationToken);

            request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.Token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
