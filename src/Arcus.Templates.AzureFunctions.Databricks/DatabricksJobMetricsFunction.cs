using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Databricks;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.AzureFunctions.Databricks
{
    /// <summary>
    /// Represents an Azure Function that will report periodically the finished Databricks job runs as metrics.
    /// </summary>
    public class DatabricksJobMetricsFunction
    {
        private readonly IConfiguration _configuration;
        private readonly ISecretProvider _secretProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabricksJobMetricsFunction"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration to retrieve required runtime information for the Databricks connection and metric reporting.</param>
        /// <param name="secretProvider">The provider to retrieve secrets for the Databricks connection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> or <paramref name="secretProvider"/> is <c>null</c>.</exception>
        public DatabricksJobMetricsFunction(IConfiguration configuration, ISecretProvider secretProvider)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires an application configuration instance to retrieve runtime information for the Databricks connection and metric reporting");
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires an secret provider instance to retrieve secrets for the Databricks connection");

            _configuration = configuration;
            _secretProvider = secretProvider;
        }

        /// <summary>
        /// Reports the finished Databricks job runs as metric.
        /// </summary>
        /// <param name="timer">The timer instance to provide information about this current scheduled run.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages and reporting metrics during the Databricks interaction.</param>
        [FunctionName("databricks-job-metrics")]
        public async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo timer, ILogger logger)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var metricName = _configuration.GetValue<string>("Arcus:ApplicationInsights:MetricName");
            var baseUrl = _configuration.GetValue<string>("Arcus:Databricks:Url");
            string secretToken = await _secretProvider.GetRawSecretAsync("Arcus.Databricks.SecretToken");

            var startOfWindow = timer.ScheduleStatus.Last;
            var endOfWindow = timer.ScheduleStatus.Next;

            using var client = DatabricksClient.CreateClient(baseUrl, secretToken);
            using (var provider = new DatabricksInfoProvider(client, logger))
            {
                await provider.MeasureJobOutcomesAsync(metricName, startOfWindow, endOfWindow);
            }
        }
    }
}
