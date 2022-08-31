using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetSuiteRestApiOAuth2.Jobs;
using Quartz;
using Serilog;
using Serilog.Events;

namespace NetSuiteRestApiOAuth2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var logfile = Path.Combine(baseDir, "Logs", "log.txt");
            const string loggerTemplate = @"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u1}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(logfile, LogEventLevel.Information, loggerTemplate,
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: 21)
                .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient<NetSuiteTokenClient>();

                    services.AddTransient<NetSuiteAuthHandler>();
                    services.AddHttpClient<NetSuiteApiClient>()
                        .AddHttpMessageHandler<NetSuiteAuthHandler>();


                    services.AddQuartz(q =>
                    {
                        q.UseMicrosoftDependencyInjectionJobFactory();

                        q.ScheduleJob<ExampleJob>(trigger => trigger
                            .WithIdentity("ExampleTrigger")
                            .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(6)))
                            .WithDailyTimeIntervalSchedule(x => x.WithInterval(10, IntervalUnit.Second))
                        );
                    });

                    services.AddQuartzHostedService(options =>
                    {
                        options.WaitForJobsToComplete = true;
                    });
                });
    }
}