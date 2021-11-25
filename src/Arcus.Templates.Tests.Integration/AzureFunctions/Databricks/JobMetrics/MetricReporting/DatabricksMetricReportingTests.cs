using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Azure.ApplicationInsights.Query;
using Microsoft.Azure.ApplicationInsights.Query.Models;
using Microsoft.Azure.Databricks.Client;
using Polly;
using Polly.Retry;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.MetricReporting
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class DatabricksMetricReportingTests : ApplicationInsightsTests
    {
        private readonly TestConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabricksMetricReportingTests"/> class.
        /// </summary>
        public DatabricksMetricReportingTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task MinimumAzureFunctionsDatabricksProject_WithEmbeddedTimer_ReportsAsMetricPeriodically()
        {
            var parameters = RunParameters.CreateNotebookParams(Enumerable.Empty<KeyValuePair<string, string>>());

            using (var project = AzureFunctionsDatabricksProject.StartNew(_config, Logger))
            using (var client = DatabricksClient.CreateClient(project.AzureFunctionDatabricksConfig.BaseUrl, project.AzureFunctionDatabricksConfig.SecurityToken))
            {
                JobSettings settings = CreateEmptyJobSettings();
                long jobId = await client.Jobs.Create(settings);

                try
                {
                    // Act
                    await client.Jobs.RunNow(jobId, parameters);
                    await WaitUntilDatabricksJobRunIsCompleted(client, jobId);
                }
                finally
                {
                    await client.Jobs.Delete(jobId);
                }
            }

            // Assert
            await RetryAssertUntilTelemetryShouldBeAvailableAsync(async client =>
            {
                MetricsPostBodySchema bodySchema = CreateMetricPostBodySchemaForDatabricksTracking();
                IList<MetricsResultsItem> results =
                    await client.Metrics.GetMultipleAsync(ApplicationInsightsConfig.ApplicationId, new List<MetricsPostBodySchema> {bodySchema});

                Assert.NotEmpty(results);
                Assert.All(results, result => Assert.NotNull(result.Body.Value));
            },
            timeout: TimeSpan.FromMinutes(2));
        }

        private static JobSettings CreateEmptyJobSettings()
        {
            var settings = new JobSettings
            {
                Name = "(temp) Arcus Templates - Integration Testing",
                NewCluster = new ClusterInfo
                {
                    RuntimeVersion = "8.3.x-scala2.12",
                    AzureAttributes = new AzureAttributes
                    {
                        Availability = AzureAvailability.ON_DEMAND_AZURE,
                        FirstOnDemand = 1,
                        SpotBidMaxPrice = -1
                    },
                    NodeTypeId = "Standard_DS3_v2",
                    SparkEnvironmentVariables = new Dictionary<string, string>
                    {
                        ["PYSPARK_PYTHON"] = "/databricks/python3/bin/python3"
                    },
                    EnableElasticDisk = true,
                    NumberOfWorkers = 8
                },
                MaxConcurrentRuns = 10,
                NotebookTask = new NotebookTask
                {
                    NotebookPath = "/Arcus - Automation"
                }
            };

            return settings;
        }

        private MetricsPostBodySchema CreateMetricPostBodySchemaForDatabricksTracking()
        {
            const string past10MinFilter = "PT0.1H";
            var bodySchema = new MetricsPostBodySchema(
                id: Guid.NewGuid().ToString(),
                parameters: new MetricsPostBodySchemaParameters(
                    $"customMetrics/{ApplicationInsightsConfig.MetricName}",
                    timespan: past10MinFilter));

            return bodySchema;
        }

        private static async Task WaitUntilDatabricksJobRunIsCompleted(DatabricksClient client, long jobId)
        {
            AsyncRetryPolicy<RunList> retryPolicy =
                Policy.HandleResult<RunList>(list => list.Runs is null || list.Runs.Any(r => !r.IsCompleted))
                      .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(10));

            await Policy.TimeoutAsync(TimeSpan.FromMinutes(10))
                        .WrapAsync(retryPolicy)
                        .ExecuteAsync(async () => await client.Jobs.RunsList(jobId));

            await Task.Delay(TimeSpan.FromMinutes(2));
        }
    }
}
