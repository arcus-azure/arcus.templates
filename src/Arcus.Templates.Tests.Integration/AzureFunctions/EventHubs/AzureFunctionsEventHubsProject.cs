using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Arcus.Templates.Tests.Integration.Worker.EventHubs;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.EventHubs
{
    /// <summary>
    /// Project template to create Azure Functions EventHubs triger projects.
    /// </summary>
    public class AzureFunctionsEventHubsProject : AzureFunctionsProject, IAsyncDisposable
    {
        private AzureFunctionsEventHubsProject(
            TestEventHubsMessageProducer messageProducer,
            TestConfig config,
            ITestOutputHelper outputWriter)
            : base(config.GetAzureFunctionsEventHubsProjectDirectory(), config, outputWriter)
        {
            MessagePump = new MessagePumpService(messageProducer, config, outputWriter);
        }

        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the Service project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public MessagePumpService MessagePump { get; }

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

        private static AzureFunctionsEventHubsProject CreateNew(
            TestConfig configuration,
            AzureFunctionsEventHubsProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            EventHubsConfig eventHubsConfig = configuration.GetEventHubsConfig();
            var producer = new TestEventHubsMessageProducer(eventHubsConfig.EventHubsName, eventHubsConfig.EventHubsConnectionString);
            var project = new AzureFunctionsEventHubsProject(producer, configuration, outputWriter);

            project.CreateNewProject(options);
            project.AddTestMessageHandler(eventHubsConfig, options);
            project.AddLocalSettings(options.FunctionsWorker);

            return project;
        }

        private void AddTestMessageHandler(EventHubsConfig eventHubsConfig, AzureFunctionsEventHubsProjectOptions options)
        {
            AddPackage("Arcus.EventGrid", "3.2.0");
            AddPackage("Arcus.EventGrid.Publishing", "3.2.0");
            AddTypeAsFile<Order>();
            AddTypeAsFile<Customer>();
            AddTypeAsFile<OrderCreatedEvent>();
            AddTypeAsFile<OrderCreatedEventData>();
            AddTypeAsFile<TestOrdersAzureEventHubsMessageHandler>();

            UpdateFileInProject("SensorReadingFunction.cs", 
                contents => contents.Replace("EventHubTrigger(\"sensors\"", $"EventHubTrigger(\"{eventHubsConfig.EventHubsName}\"")
                                    .Replace("var data = new EventData(message);", $"var data = new EventData(message);{Environment.NewLine}data.CorrelationId = properties[\"Operation-Id\"].GetString();"));


            string fileName = "";
            if (options.FunctionsWorker is FunctionsWorker.InProcess)
            {
                fileName = "Startup.cs";
            }

            if (options.FunctionsWorker is FunctionsWorker.Isolated)
            {
                fileName = "Program.cs";
            }

            UpdateFileInProject(fileName, contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace(".MinimumLevel.Debug()", ".MinimumLevel.Verbose()")
                    .Replace("SensorReadingAzureEventHubsMessageHandler", nameof(TestOrdersAzureEventHubsMessageHandler))
                    .Replace("SensorReading", nameof(Order))
                    .Replace("stores.AddAzureKeyVaultWithManagedIdentity(\"https://your-keyvault.vault.azure.net/\", CacheConfiguration.Default);", ""));
        }

        private async Task StartAsync()
        {
            EventHubsConfig eventHubsConfig = Configuration.GetEventHubsConfig();
            EventGridConfig eventGridConfig = Configuration.GetEventGridConfig();
            
            Environment.SetEnvironmentVariable("EventHubsConnectionString", eventHubsConfig.EventHubsConnectionString);
            Environment.SetEnvironmentVariable("EVENTGRID_TOPIC_URI", eventGridConfig.TopicUri);
            Environment.SetEnvironmentVariable("EVENTGRID_AUTH_KEY", eventGridConfig.AuthenticationKey);

            Run(Configuration.BuildConfiguration, TargetFramework.Net6_0);
            await MessagePump.StartAsync();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            Environment.SetEnvironmentVariable("EventHubsConnectionString", null);
            Environment.SetEnvironmentVariable("EVENTGRID_TOPIC_URI", null);
            Environment.SetEnvironmentVariable("EVENTGRID_AUTH_KEY", null);

            Dispose();
            await MessagePump.DisposeAsync();
        }
    }
}
