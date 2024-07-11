using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Project template to create Azure EventHubs worker projects.
    /// </summary>
    public class EventHubsWorkerProject : WorkerProject
    {
        private readonly TemporaryBlobStorageContainer _blobStorageContainer;

        private EventHubsWorkerProject(
            TestConfig configuration, 
            TemporaryBlobStorageContainer blobStorageContainer,
            ITestOutputHelper outputWriter)
            : base(configuration.GetEventHubsProjectDirectory(),
                   configuration,
                   outputWriter)
        {
            _blobStorageContainer = blobStorageContainer;
            Messaging = new TestEventHubsMessagePumpService(configuration, ProjectDirectory, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure EventHubs worker project template.
        /// </summary>
        /// <param name="outputWriter">The logger instance to write diagnostic information messages during the worker project creation and startup process.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static async Task<EventHubsWorkerProject> StartNewAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a logger instance to write diagnostic information messages during the worker project creation and startup process");

            var config = TestConfig.Create();
            var options = EventHubsWorkerProjectOptions.Create(config);

            return await StartNewAsync(config, options, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure EventHubs worker project template.
        /// </summary>
        /// <param name="configuration">The configuration instance to retrieve test configuration values for the used Azure resources in the worker project.</param>
        /// <param name="options">The project options to influence the contents of the worker project.</param>
        /// <param name="outputWriter">The logger instance to write diagnostic information messages during the worker project creation and startup process.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, <paramref name="options"/>, or <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static async Task<EventHubsWorkerProject> StartNewAsync(TestConfig configuration, EventHubsWorkerProjectOptions options, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve test configuration values for the used Azure resources in the worker project");
            Guard.NotNull(options, nameof(options), "Requires a project options instance to influence the contents of the worker project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a logger instance to write diagnostic information messages during the worker project creation and startup process");

            EventHubsWorkerProject project = await CreateNewAsync(configuration, options, outputWriter);
            EventHubsConfig eventHubsConfig = configuration.GetEventHubsConfig();
            
            await project.StartAsync(options,
               CommandArgument.CreateOpen("EVENTHUBS_NAME", eventHubsConfig.EventHubsName),
               CommandArgument.CreateOpen("BLOBSTORAGE_CONTAINERNAME", project._blobStorageContainer.ContainerName),
               CommandArgument.CreateSecret("ARCUS_EVENTHUBS_CONNECTIONSTRING", eventHubsConfig.EventHubsConnectionString),
               CommandArgument.CreateSecret("ARCUS_STORAGEACCOUNT_CONNECTIONSTRING", eventHubsConfig.StorageConnectionString));

            return project;
        }

        private static async Task<EventHubsWorkerProject> CreateNewAsync(
            TestConfig configuration, 
            EventHubsWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            EventHubsConfig eventHubsConfig = configuration.GetEventHubsConfig();
            var blobStorageContainer = await TemporaryBlobStorageContainer.CreateAsync(eventHubsConfig.StorageConnectionString, new XunitTestLogger(outputWriter));
            var project = new EventHubsWorkerProject(configuration, blobStorageContainer, outputWriter);

            project.CreateNewProject(options);
            project.AddTestMessageHandler();

            return project;
        }

        private void AddTestMessageHandler()
        {
            AddTypeAsFile<SensorUpdate>();
            AddTypeAsFile<SensorStatus>();
            AddTypeAsFile<SensorUpdateEventData>();
            AddTypeAsFile<WriteSensorUpdateToFileAzureEventHubsMessageHandler>();
            
            UpdateFileInProject("Program.cs", contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace(".MinimumLevel.Debug()", ".MinimumLevel.Verbose()")
                    .Replace("SensorReadingAzureEventHubsMessageHandler", nameof(WriteSensorUpdateToFileAzureEventHubsMessageHandler))
                    .Replace("SensorReading", nameof(SensorUpdate))
                    .Replace("stores.AddAzureKeyVaultWithManagedIdentity(\"https://your-keyvault.vault.azure.net/\", CacheConfiguration.Default);", ""));
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected override async ValueTask DisposingAsync(bool disposing)
        {
            await _blobStorageContainer.DisposeAsync();
        }
    }
}
