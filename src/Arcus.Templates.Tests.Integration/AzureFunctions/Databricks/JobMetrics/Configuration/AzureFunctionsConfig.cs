using System;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration
{
    /// <summary>
    /// Represents the configuration needed to run a valid Azure Functions project.
    /// </summary>
    public class AzureFunctionsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsConfig"/> class.
        /// </summary>
        /// <param name="httpPort">The HTTP port where the Azure Functions project will be running.</param>
        /// <param name="storageAccountConnectionString">The Azure web jobs storage account connection string.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="httpPort"/> is less then zero.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="storageAccountConnectionString"/> is <c>null</c>.</exception>
        public AzureFunctionsConfig(int httpPort, string storageAccountConnectionString)
        {
            Guard.NotLessThan(httpPort, 0, nameof(httpPort), "Requires a HTTP port that's greater than zero to locate the endpoint where the Azure Functions project is running");
            Guard.NotNull(storageAccountConnectionString, nameof(storageAccountConnectionString), "Requires an Azure web jobs storage account connection string to create a valid Azure Functions project");

            HttpPort = httpPort;
            StorageAccountConnectionString = storageAccountConnectionString;
        }

        /// <summary>
        /// Gets the HTTP port where the Azure Function project will be running.
        /// </summary>
        public int HttpPort { get; }

        /// <summary>
        /// Gets the Azure web jobs storage account connection string to create an valid Azure Functions project.
        /// </summary>
        public string StorageAccountConnectionString { get; }
    }
}
