using System;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Represents the available options to pass along to the <see cref="AzureFunctionsServiceBusProject"/>.
    /// </summary>
    public class AzureFunctionsServiceBusProjectOptions : ProjectOptions
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
        /// Gets the Azure Functions worker type the project should target.
        /// </summary>
        public FunctionWorker FunctionWorker { get; private set; } = FunctionWorker.InProcess;

        /// <summary>
        /// Sets the Azure Functions worker type to the project options when running the project template.
        /// </summary>
        /// <param name="workerType">The Azure Functions worker type the project should target.</param>
        public AzureFunctionsServiceBusProjectOptions WithFunctionWorker(FunctionWorker workerType)
        {
            FunctionWorker = workerType;

            if (_entityType is ServiceBusEntityType.Topic)
            {
                string workerTypeArgument = DetermineFunctionWorkerArgument(workerType);
                AddOption($"--functions-worker {workerTypeArgument}");

            }

            return this;
        }

        private static string DetermineFunctionWorkerArgument(FunctionWorker workerType)
        {
            switch (workerType)
            {
                case FunctionWorker.InProcess: return "inProcess";
                case FunctionWorker.Isolated: return "isolated";
                default:
                    throw new ArgumentOutOfRangeException(nameof(workerType), workerType, "Unknown function worker type");
            }
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
