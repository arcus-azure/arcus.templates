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
        public FunctionsWorker FunctionsWorker { get; private set; } = FunctionsWorker.InProcess;

        /// <summary>
        /// Sets the Azure Functions worker type to the project options when running the project template.
        /// </summary>
        /// <param name="workerType">The Azure Functions worker type the project should target.</param>
        public AzureFunctionsHttpProjectOptions WithFunctionWorker(FunctionsWorker workerType)
        {
            FunctionsWorker = workerType;

            string workerTypeArgument = DetermineFunctionWorkerArgument(workerType);
            AddOption($"--functions-worker {workerTypeArgument}");

            return this;
        }

        private static string DetermineFunctionWorkerArgument(FunctionsWorker workerType)
        {
            switch (workerType)
            {
                case FunctionsWorker.InProcess: return "inProcess";
                case FunctionsWorker.Isolated: return "isolated";
                default:
                    throw new ArgumentOutOfRangeException(nameof(workerType), workerType, "Unknown function worker type");
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
