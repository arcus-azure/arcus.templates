using System;
using Arcus.Templates.Tests.Integration.Fixture;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.EventHubs
{
    /// <summary>
    /// Represents the available options for the Azure Functions EventHubs projects.
    /// </summary>
    public class AzureFunctionsEventHubsProjectOptions : ProjectOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsEventHubsProjectOptions" /> class.
        /// </summary>
        public AzureFunctionsEventHubsProjectOptions()
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
        public AzureFunctionsEventHubsProjectOptions WithFunctionWorker(FunctionsWorker workerType)
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
        /// Adds the project option to exclude the Serilog logging infrastructure from the Azure Functions project.
        /// </summary>
        public AzureFunctionsEventHubsProjectOptions ExcludeSerilog()
        {
            AddOption("--exclude-serilog");
            return this;
        }
    }
}
