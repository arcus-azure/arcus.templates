using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Represents the available options to pass along to the <see cref="AzureFunctionsServiceBusProject"/>.
    /// </summary>
    public class AzureFunctionsServiceBusProjectOptions : AzureFunctionsProjectOptions
    {
        private readonly ServiceBusEntityType _entityType;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsServiceBusProjectOptions" /> class.
        /// </summary>
        public AzureFunctionsServiceBusProjectOptions(ServiceBusEntityType entityType)
        {
            _entityType = entityType;
        }

        /// <summary>
        /// Sets the Azure Functions worker type to the project options when running the project template.
        /// </summary>
        /// <param name="workerType">The Azure Functions worker type the project should target.</param>
        public AzureFunctionsServiceBusProjectOptions WithFunctionWorker(FunctionsWorker workerType)
        {
            SetFunctionsWorker(workerType);
            return this;
        }

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
