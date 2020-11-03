using System;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.Configuration
{
    /// <summary>
    /// Represents the configuration needed to run a valid Azure Functions project.
    /// </summary>
    public class AzureFunctionsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsConfig"/> class.
        /// </summary>
        /// <param name="storageAccountConnectionString">The Azure web jobs storage account connection string.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="storageAccountConnectionString"/> is <c>null</c>.</exception>
        public AzureFunctionsConfig(string storageAccountConnectionString)
        {
            Guard.NotNull(storageAccountConnectionString, nameof(storageAccountConnectionString), "Requires an Azure web jobs storage account connection string to create a valid Azure Functions project");

            StorageAccountConnectionString = storageAccountConnectionString;
        }

        /// <summary>
        /// Gets the Azure web jobs storage account connection string to create an valid Azure Functions project.
        /// </summary>
        public string StorageAccountConnectionString { get; }
    }
}
