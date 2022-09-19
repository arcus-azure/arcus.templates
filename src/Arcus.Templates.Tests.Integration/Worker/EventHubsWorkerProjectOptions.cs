using System;
using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Worker
{
    public class EventHubsWorkerProjectOptions : WorkerProjectOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsWorkerProjectOptions" /> class.
        /// </summary>
        private EventHubsWorkerProjectOptions(TestConfig config) : base(config)
        {
        }

        /// <summary>
        /// Creates an <see cref="ServiceBusWorkerProjectOptions"/> instance that provides additional user-configurable options for the Azure Service Bus .NET Worker projects.
        /// </summary>
        /// <param name="configuration">The integration test configuration instance to retrieve connection secrets.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static EventHubsWorkerProjectOptions Create(TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration instance to retrieve additional connection secrets");

            var options = new EventHubsWorkerProjectOptions(configuration);
            return options;
        }

        /// <summary>
        /// Adds the project option to exclude the Serilog logging infrastructure from the worker project.
        /// </summary>
        public EventHubsWorkerProjectOptions WithExcludeSerilog()
        {
            ExcludeSerilog();
            return this;
        }
    }
}
