using Microsoft.Extensions.Logging;
using Quartz;

namespace NetSuiteRestApiOAuth2.Jobs
{
    internal class ExampleJob : IJob
    {
        private readonly ILogger<ExampleJob> _logger;
        private readonly NetSuiteApiClient _api;

        public ExampleJob(ILogger<ExampleJob> logger, NetSuiteApiClient api)
        {
            _logger = logger;
            _api = api;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Begin ExampleJob.");

            var customer = await _api.GetCustomer("1935");
            _logger.LogInformation("Customer: {@Customer}", customer);


            _logger.LogInformation("End ExampleJob.");
        }
    }
}
