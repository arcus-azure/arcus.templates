using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Azure.ApplicationInsights.Query;
using Microsoft.Azure.ApplicationInsights.Query.Models;
using Microsoft.Azure.Databricks.Client;
using Polly;
using Polly.Retry;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.MetricReporting
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class DatabricksMetricReportingTests
    {
        private readonly TestConfig _config;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabricksMetricReportingTests"/> class.
        /// </summary>
        public DatabricksMetricReportingTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task MinimumAzureFunctionsDatabricksProject_WithEmbeddedTimer_ReportsAsMetricPeriodically()
        {
            ApplicationInsightsConfig applicationInsightsConfig = _config.GetApplicationInsightsConfig();
            var parameters = RunParameters.CreateNotebookParams(Enumerable.Empty<KeyValuePair<string, string>>());

            using (var project = AzureFunctionsDatabricksProject.StartNew(_config, _outputWriter))
            {
                using (var client = DatabricksClient.CreateClient(project.DatabricksConfig.BaseUrl, project.DatabricksConfig.SecurityToken))
                {
                    // Act
                    await client.Jobs.RunNow(project.DatabricksConfig.JobId, parameters);
                    await WaitUntilDatabricksJobRunIsCompleted(client, project.DatabricksConfig.JobId);
                }
            }

            // Assert
            using (ApplicationInsightsDataClient client = CreateApplicationInsightsClient(applicationInsightsConfig.ApiKey))
            {
                await RetryAssertUntilTelemetryShouldBeAvailableAsync(async () =>
                {
                    const string past10MinFilter = "PT0.1H";
                    var bodySchema = new MetricsPostBodySchema(
                        id: Guid.NewGuid().ToString(),
                     parameters: new MetricsPostBodySchemaParameters($"customMetrics/{applicationInsightsConfig.MetricName}", timespan: past10MinFilter));

                    IList<MetricsResultsItem> results =
                        await client.Metrics.GetMultipleAsync(applicationInsightsConfig.ApplicationId, new List<MetricsPostBodySchema> { bodySchema });

                    Assert.NotEmpty(results);
                    Assert.All(results, result => Assert.NotNull(result.Body.Value));

                }, timeout: TimeSpan.FromMinutes(2));
            }
        }

        private static async Task WaitUntilDatabricksJobRunIsCompleted(DatabricksClient client, int jobId)
        {
            AsyncRetryPolicy<RunList> retryPolicy =
                Policy.HandleResult<RunList>(list => list.Runs.Any(r => !r.IsCompleted))
                      .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(10));

            await Policy.TimeoutAsync(TimeSpan.FromMinutes(7))
                        .WrapAsync(retryPolicy)
                        .ExecuteAsync(async () => await client.Jobs.RunsList(jobId, activeOnly: true));

            await Task.Delay(TimeSpan.FromMinutes(2));
        }

        private static ApplicationInsightsDataClient CreateApplicationInsightsClient(string apiKey)
        {
            var clientCredentials = new ApiKeyClientCredentials(apiKey);
            var client = new ApplicationInsightsDataClient(clientCredentials);

            return client;
        }

        private async Task RetryAssertUntilTelemetryShouldBeAvailableAsync(Func<Task> assertion, TimeSpan timeout)
        {
            AsyncRetryPolicy retryPolicy =
                Policy.Handle<Exception>(exception =>
                      {
                          _outputWriter.WriteLine("Failed to contact Azure Application Insights. Reason: {0}", exception.Message);
                          return true;
                      })
                      .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(1));

            await Policy.TimeoutAsync(timeout)
                        .WrapAsync(retryPolicy)
                        .ExecuteAsync(assertion);
        }
    }
}
