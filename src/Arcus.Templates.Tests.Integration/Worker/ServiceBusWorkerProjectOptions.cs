using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Represents the available options for the Azure Service Bus Topic and Queue worker projects.
    /// </summary>
    public class ServiceBusWorkerProjectOptions : ProjectOptions
    {
        private const string SerilogTelemetryInstrumentationKey = "Telemetry_ApplicationInsights_InstrumentationKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusWorkerProjectOptions"/> class.
        /// </summary>
        private ServiceBusWorkerProjectOptions(IEnumerable<CommandArgument> additionalArguments)
        {
            Guard.NotNull(additionalArguments, nameof(additionalArguments), "Requires a non-null collection of additional arguments");
            Guard.For<ArgumentException>(() => additionalArguments.Any(arg => arg is null), "Requires all additional arguments to be not 'null'");
            
            AdditionalArguments = additionalArguments;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusWorkerProjectOptions"/> class.
        /// </summary>
        private ServiceBusWorkerProjectOptions(
            IEnumerable<CommandArgument> additionalArguments,
            ProjectOptions options) : base(options)
        {
            Guard.NotNull(additionalArguments, nameof(additionalArguments), "Requires a non-null collection of additional arguments");
            Guard.For<ArgumentException>(() => additionalArguments.Any(arg => arg is null), "Requires all additional arguments to be not 'null'");

            AdditionalArguments = additionalArguments;
        }

        /// <summary>
        /// Gets the additional arguments for the Service Bus worker project.
        /// </summary>
        public IEnumerable<CommandArgument> AdditionalArguments { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static ServiceBusWorkerProjectOptions Create(TestConfig configuration)
        {
            string instrumentationKey = configuration.GetApplicationInsightsInstrumentationKey();
            var commandArgument = CommandArgument.CreateSecret(SerilogTelemetryInstrumentationKey, instrumentationKey);

            return new ServiceBusWorkerProjectOptions(new[] { commandArgument });
        }

        /// <summary>
        /// Adds the project option to exclude the Serilog logging infrastructure from the worker project.
        /// </summary>
        public ServiceBusWorkerProjectOptions WithExcludeSerilog()
        {
            ProjectOptions optionsWithoutSerilog = AddOption("--exclude-serilog");
            IEnumerable<CommandArgument> argumentsWithoutSerilog = AdditionalArguments.Where(arg => arg.Name != SerilogTelemetryInstrumentationKey);
            
            return new ServiceBusWorkerProjectOptions(argumentsWithoutSerilog, optionsWithoutSerilog);
        }
    }
}
