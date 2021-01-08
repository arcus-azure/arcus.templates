using System;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration
{
    /// <summary>
    /// Represents the Azure Databricks configuration used during the integration test suite.
    /// </summary>
    public class AzureFunctionDatabricksConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionDatabricksConfig"/> class.
        /// </summary>
        /// <param name="httpPort">The HTTP port where the Azure Functions project will be running.</param>
        /// <param name="baseUrl">The Databricks base URL to locate to the Azure Databricks resource.</param>
        /// <param name="securityToken">The Databricks security token to authenticate with the Azure Databricks resource.</param>
        /// <param name="jobId">The Databricks job to use while interacting with the Databricks resource.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="httpPort"/> is less then zero.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="baseUrl"/> or <paramref name="securityToken"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="jobId"/> is less then zero.</exception>
        public AzureFunctionDatabricksConfig(int httpPort, string baseUrl, string securityToken, int jobId)
        {
            Guard.NotLessThan(httpPort, 0, nameof(httpPort), "Requires a HTTP port that's greater than zero to locate the endpoint where the Azure Functions project is running");
            Guard.NotNullOrWhitespace(baseUrl, nameof(baseUrl), "Requires a non-blank Databricks base URL for the integration test configuration");
            Guard.NotNullOrWhitespace(securityToken, nameof(securityToken), "Requires a non-blank Databricks security token for the integration test configuration");
            Guard.NotLessThan(jobId, 0, nameof(jobId), "Requires a Databricks job ID that's greater than zero");

            HttpPort = httpPort;
            BaseUrl = baseUrl;
            SecurityToken = securityToken;
            JobId = jobId;
        }
        
        /// <summary>
        /// Gets the HTTP port where the Azure Function project will be running.
        /// </summary>
        public int HttpPort { get; }

        /// <summary>
        /// Gets the Databricks base URL to locate the Azure Databricks resource.
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>
        /// Gets the Databricks security token to authenticate with the Azure Databricks resource.
        /// </summary>
        public string SecurityToken { get; }

        /// <summary>
        /// Gets the ID of the Databricks job that's being used to interact with the Azure Databricks resource.
        /// </summary>
        public int JobId { get; }
    }
}
