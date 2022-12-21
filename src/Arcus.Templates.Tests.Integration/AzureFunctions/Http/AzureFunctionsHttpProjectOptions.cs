using System;
using Arcus.Templates.Tests.Integration.Fixture;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http
{
    /// <summary>
    /// Represents the additional consumer options for the <see cref="AzureFunctionsHttpProject"/>.
    /// </summary>
    public class AzureFunctionsHttpProjectOptions : ProjectOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsHttpProjectOptions" /> class.
        /// </summary>
        public AzureFunctionsHttpProjectOptions()
        {
        }

        /// <summary>
        /// Gets the Azure Functions worker type the project should target.
        /// </summary>
        public FunctionsWorker FunctionsWorker { get; private set; } = FunctionsWorker.Isolated;

        /// <summary>
        /// Adds the project option to configure the Azure Functions worker type of the Azure Functions HTTP trigger project.
        /// </summary>
        /// <param name="workerType">The type of functions worker to use when setting up the Azure Functions HTTP trigger project.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="workerType"/> is outside the bounds of the enumeration.</exception>
        public AzureFunctionsHttpProjectOptions WithFunctionsWorker(FunctionsWorker workerType)
        {
            FunctionsWorker = workerType;

            string workerTyperArgument = DetermineFunctionsWorkerArgument(workerType);
            AddOption($"--functions-worker {workerTyperArgument}");

            return this;
        }

        private static string DetermineFunctionsWorkerArgument(FunctionsWorker workerType)
        {
            switch (workerType)
            {
                case FunctionsWorker.InProcess: return "inProcess";
                case FunctionsWorker.Isolated: return "isolated";
                default:
                    throw new ArgumentOutOfRangeException(nameof(workerType), workerType, "Unknown functions worker type");
            }
        }

        /// <summary>
        /// Adds the project option to include the health checks Azure Function from the Azure Functions HTTP trigger project.
        /// </summary>
        public AzureFunctionsHttpProjectOptions WithIncludeHealthChecks()
        {
            AddOption("--include-healthchecks");
            return this;
        }
        
        /// <summary>
        /// Adds the project option to exclude the correlation capability to the Azure Functions HTTP trigger project.
        /// </summary>
        public AzureFunctionsHttpProjectOptions WithExcludeOpenApiDocs()
        {
            AddOption("--exclude-openApi");
            return this;
        }

        /// <summary>
        /// Adds the project option to exclude the Serilog logging system from the Azure Functions HTTP trigger project.
        /// </summary>
        public AzureFunctionsHttpProjectOptions WithExcludeSerilog()
        {
            AddOption("--exclude-serilog");
            return this;
        }
    }
}
