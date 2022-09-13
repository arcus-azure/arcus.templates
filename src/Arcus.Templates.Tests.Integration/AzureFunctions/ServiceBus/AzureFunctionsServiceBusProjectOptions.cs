using Arcus.Templates.Tests.Integration.Fixture;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Represents the available options to pass along to the <see cref="AzureFunctionsServiceBusProject"/>.
    /// </summary>
    public class AzureFunctionsServiceBusProjectOptions : ProjectOptions
    {
        private AzureFunctionsServiceBusProjectOptions(ProjectOptions options) 
            : base(options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsServiceBusProjectOptions" /> class.
        /// </summary>
        public AzureFunctionsServiceBusProjectOptions()
        {
        }

        /// <summary>
        /// Adds the 'exclude Serilog' project options when running the project template.
        /// </summary>
        public AzureFunctionsServiceBusProjectOptions WithExcludeSerilog()
        {
            ProjectOptions options = AddOption("--exclude-serilog");
            return new AzureFunctionsServiceBusProjectOptions(options);
        }
    }
}
