using Arcus.Templates.Tests.Integration.Fixture;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http
{
    /// <summary>
    /// Represents the additional consumer options for the <see cref="AzureFunctionsHttpProject"/>.
    /// </summary>
    public class AzureFunctionsHttpProjectOptions : ProjectOptions
    {
        private AzureFunctionsHttpProjectOptions(ProjectOptions existingOptions) : base(existingOptions)
        {
        }

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
            ProjectOptions newOptions = AddOption("--include-healthchecks");
            return new AzureFunctionsHttpProjectOptions(newOptions);
        }
    }
}
