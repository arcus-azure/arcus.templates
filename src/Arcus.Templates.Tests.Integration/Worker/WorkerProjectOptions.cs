using System;
using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Represents a all available common project options across worker project templates.
    /// </summary>
    public abstract class WorkerProjectOptions : ProjectOptions
    {
        private const string ApplicationInsightsConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerProjectOptions"/> class.
        /// </summary>
        /// <param name="config">The test configuration instance containing the common values across worker project templates.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> is <c>null</c>.</exception>
        protected WorkerProjectOptions(TestConfig config)
        {
            Guard.NotNull(config, nameof(config), "Requires a configuration instance to retrieve the common test configuration values across worker project templates");

            string instrumentationKey = config.GetApplicationInsightsInstrumentationKey();
            var commandArgument = CommandArgument.CreateSecret(ApplicationInsightsConnectionStringKey, $"InstrumentationKey={instrumentationKey}");
            AddRunArgument(commandArgument);
        }

        /// <summary>
        /// Removes any Serilog relationship from the worker project.
        /// </summary>
        protected void ExcludeSerilog()
        {
            AddOption("--exclude-serilog");
            RemoveRunArgument(ApplicationInsightsConnectionStringKey);
        }
    }
}