using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Represents the available options for the Azure Service Bus Topic and Queue worker projects.
    /// </summary>
    public class WorkerMessagingProjectOptions : ProjectOptions
    {
        private const string ApplicationInsightsConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

        private WorkerMessagingProjectOptions(
            ServiceBusEntityType entityType,
            IEnumerable<CommandArgument> additionalArguments)
        {
            Guard.NotNull(additionalArguments, nameof(additionalArguments), "Requires a non-null collection of additional arguments");
            Guard.For<ArgumentException>(() => additionalArguments.Any(arg => arg is null), "Requires all additional arguments to be not 'null'");

            ServiceBusEntityType = entityType;
            AdditionalArguments = additionalArguments;
        }

        private WorkerMessagingProjectOptions(
            ServiceBusEntityType entityType,
            IEnumerable<CommandArgument> additionalArguments,
            ProjectOptions options) : base(options)
        {
            Guard.NotNull(additionalArguments, nameof(additionalArguments), "Requires a non-null collection of additional arguments");
            Guard.For<ArgumentException>(() => additionalArguments.Any(arg => arg is null), "Requires all additional arguments to be not 'null'");

            ServiceBusEntityType = entityType;
            AdditionalArguments = additionalArguments;
        }

        public ServiceBusEntityType ServiceBusEntityType { get; }

        /// <summary>
        /// Gets the additional arguments for the Service Bus worker project.
        /// </summary>
        public IEnumerable<CommandArgument> AdditionalArguments { get; }

        /// <summary>
        /// Creates an <see cref="WorkerMessagingProjectOptions"/> instance that provides additional user-configurable options for the Azure Service Bus .NET Worker projects.
        /// </summary>
        /// <param name="configuration">The integration test configuration instance to retrieve connection secrets.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static WorkerMessagingProjectOptions CreateForServiceBusQueue(TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration instance to retrieve additional connection secrets");

            return CreateForServiceBus(ServiceBusEntityType.Queue, configuration);
        }

        /// <summary>
        /// Creates an <see cref="WorkerMessagingProjectOptions"/> instance that provides additional user-configurable options for the Azure Service Bus .NET Worker projects.
        /// </summary>
        /// <param name="configuration">The integration test configuration instance to retrieve connection secrets.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static WorkerMessagingProjectOptions CreateForServiceBusTopic(TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration instance to retrieve additional connection secrets");

            return CreateForServiceBus(ServiceBusEntityType.Topic, configuration);
        }

        /// <summary>
        /// Creates an <see cref="WorkerMessagingProjectOptions"/> instance that provides additional user-configurable options for the Azure Service Bus .NET Worker projects.
        /// </summary>
        /// <param name="entityType">The type of the Azure Service Bus entity that should be used as messaging source in the worker messaging project.</param>
        /// <param name="configuration">The integration test configuration instance to retrieve connection secrets.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static WorkerMessagingProjectOptions CreateForServiceBus(ServiceBusEntityType entityType, TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration instance to retrieve additional connection secrets");

            return CreateOptionsWithApplicationInsights(entityType, configuration)
                .AddMessagingOptions(entityType);
        }

        private static WorkerMessagingProjectOptions CreateOptionsWithApplicationInsights(ServiceBusEntityType entityType, TestConfig configuration)
        {
            string instrumentationKey = configuration.GetApplicationInsightsInstrumentationKey();
            var commandArgument = CommandArgument.CreateSecret(ApplicationInsightsConnectionStringKey, $"InstrumentationKey={instrumentationKey}");

            return new WorkerMessagingProjectOptions(entityType, new[] { commandArgument });
        }

        private WorkerMessagingProjectOptions AddMessagingOptions(ServiceBusEntityType entityType)
        {
            string messagingSource = DetermineMessagingSourceForEntityType(entityType);
            ProjectOptions options = AddOption($"--messaging {messagingSource}");

            return new WorkerMessagingProjectOptions(ServiceBusEntityType, AdditionalArguments, options);
        }

        private static string DetermineMessagingSourceForEntityType(ServiceBusEntityType entityType)
        {
            switch (entityType)
            {
                case ServiceBusEntityType.Queue:
                    return "ServiceBusQueue";
                case ServiceBusEntityType.Topic:
                    return "ServiceBusTopic";
                default:
                    throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Azure Service Bus entity type");
            }
        }

        /// <summary>
        /// Adds the project option to exclude the Serilog logging infrastructure from the worker project.
        /// </summary>
        public WorkerMessagingProjectOptions WithExcludeSerilog()
        {
            ProjectOptions optionsWithoutSerilog = AddOption("--exclude-serilog");
            IEnumerable<CommandArgument> argumentsWithoutSerilog = AdditionalArguments.Where(arg => arg.Name != ApplicationInsightsConnectionStringKey);
            
            return new WorkerMessagingProjectOptions(ServiceBusEntityType, argumentsWithoutSerilog, optionsWithoutSerilog);
        }
    }
}
