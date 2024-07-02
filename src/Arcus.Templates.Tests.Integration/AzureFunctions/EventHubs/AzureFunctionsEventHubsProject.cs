using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Admin;
using Arcus.Templates.Tests.Integration.AzureFunctions.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.EventHubs
{
    /// <summary>
    /// Project template to create Azure Functions EventHubs trigger projects.
    /// </summary>
    public class AzureFunctionsEventHubsProject : AzureFunctionsProject
    {
        private AzureFunctionsEventHubsProject(
            TestConfig config,
            AzureFunctionsEventHubsProjectOptions options,
            ITestOutputHelper outputWriter)
            : base(config.GetAzureFunctionsEventHubsProjectDirectory(), config, options, outputWriter)
        {
            Messaging = new TestEventHubsMessagePumpService(config, ProjectDirectory, outputWriter);
            Admin = new AdminEndpointService(RootEndpoint.Port, "sensor-reading", outputWriter);
        }

        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the Service project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public IMessagingService Messaging { get; }

        /// <summary>
        /// Gets the service to run administrative actions on the Azure Functions project.
        /// </summary>
        public AdminEndpointService Admin { get; }

        /// <summary>
        /// Starts a newly created project from the Azure Functions EventHubs project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions EventHubs project with a set of services to interact with the project.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static async Task<AzureFunctionsEventHubsProject> StartNewAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsEventHubsProject project = await StartNewAsync(TestConfig.Create(), new AzureFunctionsEventHubsProjectOptions(), outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions EventHubs project template.
        /// </summary>
        /// <param name="options">The additional project options to pass along to the project creation command.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions EventHubs project with a set of services to interact with the project.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static async Task<AzureFunctionsEventHubsProject> StartNewAsync(
            TestConfig configuration,
            AzureFunctionsEventHubsProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of project options to pass along to the project creation command");
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsEventHubsProject project = CreateNew(configuration, options, outputWriter);
            await project.StartAsync();

            return project;
        }

        /// <summary>
        /// Creates a project from the Azure Functions EventHubs project template.
        /// </summary>
        /// <param name="options">The additional project options to pass along to the project creation command.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions EventHubs project with a set of services to interact with the project.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static AzureFunctionsEventHubsProject CreateNew(
            TestConfig configuration,
            AzureFunctionsEventHubsProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of project options to pass along to the project creation command");
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation process");

            EventHubsConfig eventHubsConfig = configuration.GetEventHubsConfig();
            var project = new AzureFunctionsEventHubsProject(configuration, options, outputWriter);

            project.CreateNewProject(options);
            project.AddTestMessageHandler();
            project.AddLocalSettings();

            ApplicationInsightsConfig appInsightsConfig = configuration.GetApplicationInsightsConfig();
            project.AddLocalSettings(new Dictionary<string, string>
            {
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = $"InstrumentationKey={appInsightsConfig.InstrumentationKey}",
                ["EventHubsConnectionString"] = eventHubsConfig.EventHubsConnectionString
            });

            Environment.SetEnvironmentVariable("EventHubsConnectionString", eventHubsConfig.EventHubsConnectionString);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", $"InstrumentationKey={appInsightsConfig.InstrumentationKey}");

            return project;
        }

        private void AddTestMessageHandler()
        {
            AddTypeAsFile<SensorUpdate>();
            AddTypeAsFile<SensorStatus>();
            AddTypeAsFile<SensorUpdateEventData>();
            AddTypeAsFile<WriteSensorUpdateToFileAzureEventHubsMessageHandler>();

            UpdateFileInProject(RuntimeFileName, contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace(".MinimumLevel.Debug()", ".MinimumLevel.Verbose()")
                    .Replace("SensorReadingAzureEventHubsMessageHandler", nameof(WriteSensorUpdateToFileAzureEventHubsMessageHandler))
                    .Replace("SensorReading", nameof(SensorUpdate))
                    .Replace("stores.AddAzureKeyVaultWithManagedIdentity(\"https://your-keyvault.vault.azure.net/\", CacheConfiguration.Default);", ""));
        }

        private async Task StartAsync()
        {
            try
            {
                Run(Configuration.BuildConfiguration, TargetFramework.Net8_0);
                await WaitUntilTriggerIsAvailableAsync(Admin.Endpoint);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected override void Disposing(bool disposing)
        {
            base.Disposing(disposing);

            Environment.SetEnvironmentVariable("EventHubsConnectionString", null);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);
        }
    }
}
