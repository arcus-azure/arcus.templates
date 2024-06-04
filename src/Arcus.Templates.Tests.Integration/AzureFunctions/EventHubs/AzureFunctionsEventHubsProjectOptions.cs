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
        /// Adds the project option to exclude the Serilog logging infrastructure from the Azure Functions project.
        /// </summary>
        public AzureFunctionsEventHubsProjectOptions ExcludeSerilog()
        {
            AddOption("--exclude-serilog");
            return this;
        }
    }
}
