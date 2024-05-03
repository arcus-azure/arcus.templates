namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http
{
    /// <summary>
    /// Represents the additional consumer options for the <see cref="AzureFunctionsHttpProject"/>.
    /// </summary>
    public class AzureFunctionsHttpProjectOptions : AzureFunctionsProjectOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsHttpProjectOptions" /> class.
        /// </summary>
        public AzureFunctionsHttpProjectOptions()
        {
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
