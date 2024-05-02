using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Configuration;
using GuardNet;
using Microsoft.Azure.ApplicationInsights.Query;
using Polly;
using Polly.Retry;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents an integration test class that uses Azure Application Insights to assert the test result.
    /// </summary>
    public abstract class ApplicationInsightsTests
    {
        /// <summary>
        /// Gets the timespan statement that limits the query result to only telemetry from the last hour.
        /// </summary>
        protected const string PastHalfHourTimeSpan = "PT30M";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsTests" /> class.
        /// </summary>
        /// <param name="outputWriter">The logger instance to write diagnostic trace messages during the interaction with Azure Application Insights.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        protected ApplicationInsightsTests(ITestOutputHelper outputWriter) : this(TestConfig.Create(), outputWriter)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsTests" /> class.
        /// </summary>
        /// <param name="configuration">The test configuration used during the tests.</param>
        /// <param name="outputWriter">The logger instance to write diagnostic trace messages during the interaction with Azure Application Insights.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> or the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        protected ApplicationInsightsTests(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a logger instance to write diagnostic trace messages during the interaction with Azure Application Insights");

            Configuration = configuration;
            ApplicationInsightsConfig = Configuration.GetApplicationInsightsConfig();
            Logger = outputWriter;
        }

        /// <summary>
        /// Gets the configuration used during the tests.
        /// </summary>
        protected TestConfig Configuration { get; }

        /// <summary>
        /// Gets the sub-set of the test <see cref="Configuration"/> that includes all the Azure Application Insights information.
        /// </summary>
        protected ApplicationInsightsConfig ApplicationInsightsConfig { get; }
        
        /// <summary>
        /// Gets the instance to write diagnostic messages.
        /// </summary>
        protected ITestOutputHelper Logger { get; }

        /// <summary>
        /// Reliable assertion that retries an <paramref name="assertion"/> until the assert result succeeds or the <paramref name="timeout"/> expires.
        /// </summary>
        /// <param name="assertion">The test assertion to verify custom results.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="assertion"/> is <c>null</c>.</exception>
        public async Task RetryAssertUntilTelemetryShouldBeAvailableAsync(Func<ApplicationInsightsDataClient, Task> assertion)
        {
            await RetryAssertUntilTelemetryShouldBeAvailableAsync(assertion, timeout: TimeSpan.FromMinutes(7));
        }
        
        /// <summary>
        /// Reliable assertion that retries an <paramref name="assertion"/> until the assert result succeeds or the <paramref name="timeout"/> expires.
        /// </summary>
        /// <param name="assertion">The test assertion to verify custom results.</param>
        /// <param name="timeout">The maximum time range the <paramref name="assertion"/> is allowed to be retried.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="assertion"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/> is a negative time span.</exception>
        protected async Task RetryAssertUntilTelemetryShouldBeAvailableAsync(Func<ApplicationInsightsDataClient, Task> assertion, TimeSpan timeout)
        {
            Guard.NotNull(assertion, nameof(assertion), "Requires a test assertion to verify custom results");
            Guard.NotLessThan(timeout, TimeSpan.Zero, nameof(timeout), "Requires a positive time span for the timeout");

            using (ApplicationInsightsDataClient client = CreateApplicationInsightsClient())
            {
                AsyncRetryPolicy retryPolicy =
                    Policy.Handle<Exception>(exception =>
                          {
                              Logger.WriteLine("Failed to contact Azure Application Insights. Reason: {0}", exception);
                              return true;
                          })
                          .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(3));

                await Policy.TimeoutAsync(timeout)
                            .WrapAsync(retryPolicy)
                            .ExecuteAsync(() => assertion(client));
            }
        }

        private ApplicationInsightsDataClient CreateApplicationInsightsClient()
        {
            var clientCredentials = new ApiKeyClientCredentials(ApplicationInsightsConfig.ApiKey);
            var client = new ApplicationInsightsDataClient(clientCredentials);

            return client;
        }
    }
}
