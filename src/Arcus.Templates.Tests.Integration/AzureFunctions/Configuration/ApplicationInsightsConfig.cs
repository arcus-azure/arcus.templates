using GuardNet;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Configuration
{
    /// <summary>
    /// Represents an application configuration section related to information regarding Azure Application Insights.
    /// </summary>
    public class ApplicationInsightsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsConfig"/> class.
        /// </summary>
        /// <param name="instrumentationKey">The instrumentation key of the Azure Application Insights resource.</param>
        /// <param name="applicationId">The application ID that has API access to the Azure Application Insights resource.</param>
        /// <param name="apiKey">The application API key that has API access to the Azure Application Insights resource.</param>
        /// <param name="metricName">The custom metric name to use when interacting with the Application Insights resource.</param>
        /// <exception cref="System.ArgumentException">Thrown when the <paramref name="instrumentationKey"/> or <paramref name="apiKey"/> or <paramref name="apiKey"/> or <paramref name="metricName"/> is blank.</exception>
        public ApplicationInsightsConfig(string instrumentationKey, string applicationId, string apiKey, string metricName)
        {
            Guard.NotNullOrWhitespace(instrumentationKey, nameof(instrumentationKey), "Requires a non-blank Application Insights instrumentation key");
            Guard.NotNullOrWhitespace(apiKey, nameof(apiKey), "Requires a non-blank Application Insights application application ID");
            Guard.NotNullOrWhitespace(apiKey, nameof(apiKey), "Requires a non-blank Application Insights application API key");
            Guard.NotNullOrWhitespace(metricName, nameof(metricName), "Requires a non-blank name to track the custom metric");

            InstrumentationKey = instrumentationKey;
            ApplicationId = applicationId;
            ApiKey = apiKey;
            MetricName = metricName;
        }

        /// <summary>
        /// Gets the instrumentation key to connect to the Application Insights resource.
        /// </summary>
        public string InstrumentationKey { get; }

        /// <summary>
        /// Gets the application ID which has API access to the Application Insights resource.
        /// </summary>
        public string ApplicationId { get; }

        /// <summary>
        /// Gets the application API key which has API access to the Application Insights resource.
        /// </summary>
        public string ApiKey { get; }

        /// <summary>
        /// Gets the custom metric name to track the metric on the Application Insights resource.
        /// </summary>
        public string MetricName { get; }
    }
}
