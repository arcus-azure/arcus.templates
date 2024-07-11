using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Admin;
using Arcus.Templates.Tests.Integration.AzureFunctions.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture;
using Arcus.Testing;
using GuardNet;
using Xunit.Abstractions;
using TestConfig = Arcus.Templates.Tests.Integration.Fixture.TestConfig;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.EventHubs
{
    /// <summary>
    /// Project template to create Azure Functions EventHubs trigger projects.
    /// </summary>
    public class AzureFunctionsEventHubsProject : AzureFunctionsProject, IAsyncDisposable
    {
        private AzureFunctionsEventHubsProject(
            TestConfig config,
            AzureFunctionsEventHubsProjectOptions options,
            ITestOutputHelper outputWriter)
            : base(config.GetAzureFunctionsEventHubsProjectDirectory(), config, options, outputWriter)
        {
            Admin = new AdminEndpointService(RootEndpoint.Port, "sensor-reading", outputWriter);
        }

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

            var project = new AzureFunctionsEventHubsProject(configuration, options, outputWriter);
            project.CreateNewProject(options);
            project.AddLocalSettings();

            project.UpdateFileInProject(project.RuntimeFileName,
                contents => project.RemovesUserErrorsFromContents(contents)
                                   .Replace("stores.AddAzureKeyVaultWithManagedIdentity(\"https://your-keyvault.vault.azure.net/\", CacheConfiguration.Default);", ""));

            return project;
        }

        private async Task StartAsync()
        {
            try
            {
                EventHubsConfig eventHubsConfig = Configuration.GetEventHubsConfig();

                Environment.SetEnvironmentVariable("EventHubsConnectionString", eventHubsConfig.EventHubsConnectionString);

                ApplicationInsightsConfig appInsightsConfig = Configuration.GetApplicationInsightsConfig();
                Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", $"InstrumentationKey={appInsightsConfig.InstrumentationKey}");

                Run(Configuration.BuildConfiguration, TargetFramework.Net8_0);

                await Poll.UntilAvailableAsync(() => Admin.TriggerFunctionAsync());
            }
            catch
            {
                await DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            Environment.SetEnvironmentVariable("EventHubsConnectionString", null);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);

            Dispose();
        }
    }
}
