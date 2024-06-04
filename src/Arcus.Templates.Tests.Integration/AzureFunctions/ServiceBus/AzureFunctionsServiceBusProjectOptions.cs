using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Represents the available options to pass along to the <see cref="AzureFunctionsServiceBusProject"/>.
    /// </summary>
    public class AzureFunctionsServiceBusProjectOptions : AzureFunctionsProjectOptions
    {
        /// <summary>
        /// Adds the 'exclude Serilog' project options when running the project template.
        /// </summary>
        public AzureFunctionsServiceBusProjectOptions WithExcludeSerilog()
        {
            AddOption("--exclude-serilog");
            return this;
        }
    }
}
