namespace Arcus.Templates.Tests.Integration.AzureFunctions.EventHubs
{
    /// <summary>
    /// Represents the available options for the Azure Functions EventHubs projects.
    /// </summary>
    public class AzureFunctionsEventHubsProjectOptions : AzureFunctionsProjectOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsEventHubsProjectOptions" /> class.
        /// </summary>
        public AzureFunctionsEventHubsProjectOptions()
        {
        }

        /// <summary>
        /// Sets the Azure Functions worker type to the project options when running the project template.
        /// </summary>
        /// <param name="workerType">The Azure Functions worker type the project should target.</param>
        public AzureFunctionsEventHubsProjectOptions WithFunctionWorker(FunctionsWorker workerType)
        {
            SetFunctionsWorker(workerType);
            return this;
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
