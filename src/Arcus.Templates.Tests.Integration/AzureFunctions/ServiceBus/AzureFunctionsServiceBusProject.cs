using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Project template to create new Azure Functions Service Bus projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}")]
    public class AzureFunctionsServiceBusProject : AzureFunctionsProject, IAsyncDisposable
    {
        private AzureFunctionsServiceBusProject(
            ServiceBusEntity entity, 
            TestConfig configuration, 
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsServiceBusProjectDirectory(entity), 
                   configuration, 
                   outputWriter)
        {
            MessagePump = new MessagePumpService(entity, configuration, outputWriter);
        }

        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the Azure Functions Service Bus project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public MessagePumpService MessagePump { get; }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus Queue project template.
        /// </summary>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions Service Bus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<AzureFunctionsServiceBusProject> StartNewQueueProjectAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            return await StartNewQueueProjectAsync(new AzureFunctionsServiceBusProjectOptions(), configuration, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus Queue project template.
        /// </summary>
        /// <param name="options">The additional project options to pass along to the project creation command.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions Service Bus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<AzureFunctionsServiceBusProject> StartNewQueueProjectAsync(
            AzureFunctionsServiceBusProjectOptions options, 
            TestConfig configuration, 
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsServiceBusProject project = CreateNew(ServiceBusEntity.Queue, options, configuration, outputWriter);

            await project.StartAsync(ServiceBusEntity.Queue);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus Topic project template.
        /// </summary>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions Service Bus Topic project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<AzureFunctionsServiceBusProject> StartNewTopicProjectAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsServiceBusProject project = CreateNew(ServiceBusEntity.Topic, new AzureFunctionsServiceBusProjectOptions(), configuration, outputWriter);

            await project.StartAsync(ServiceBusEntity.Topic);
            return project;
        }

        private static AzureFunctionsServiceBusProject CreateNew(
            ServiceBusEntity entity, 
            AzureFunctionsServiceBusProjectOptions options, 
            TestConfig configuration, 
            ITestOutputHelper outputWriter)
        {
            var project = new AzureFunctionsServiceBusProject(entity, configuration, outputWriter);
            project.CreateNewProject(options);
            project.AddOrderMessageHandlerImplementation();
            project.AddStorageAccount();

            return project;
        }

        private void AddOrderMessageHandlerImplementation()
        {
            AddPackage("Arcus.EventGrid", "3.2.0");
            AddPackage("Arcus.EventGrid.Publishing", "3.2.0");

            AddTypeAsFile<Order>();
            AddTypeAsFile<Customer>();
            AddTypeAsFile<OrderCreatedEvent>();
            AddTypeAsFile<OrderCreatedEventData>();
            AddTypeAsFile<OrdersMessageHandler>();
            UpdateFileInProject("Startup.cs", contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace("OrdersAzureServiceBusMessageHandler", nameof(OrdersMessageHandler)));
        }

        private async Task StartAsync(ServiceBusEntity entity)
        {
            string serviceBusConnectionString = Configuration.GetServiceBusConnectionString(entity);
            var properties = ServiceBusConnectionStringProperties.Parse(serviceBusConnectionString);
            string namespaceConnectionString = $"Endpoint={properties.Endpoint};SharedAccessKeyName={properties.SharedAccessKeyName};SharedAccessKey={properties.SharedAccessKey}";
            Environment.SetEnvironmentVariable("ServiceBusConnectionString", namespaceConnectionString);

            if (entity is ServiceBusEntity.Topic)
            {
                await AddServiceBusTopicSubscriptionAsync(properties.EntityPath, namespaceConnectionString);
            }

            EventGridConfig eventGridConfig = Configuration.GetEventGridConfig();
            Environment.SetEnvironmentVariable("EVENTGRID_TOPIC_URI", eventGridConfig.TopicUri);
            Environment.SetEnvironmentVariable("EVENTGRID_AUTH_KEY", eventGridConfig.AuthenticationKey);

            string instrumentationKey = Configuration.GetApplicationInsightsInstrumentationKey();
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", instrumentationKey);

            Run(BuildConfiguration.Release, TargetFramework.Net6_0);
            await MessagePump.StartAsync();
        }

        private static async Task AddServiceBusTopicSubscriptionAsync(string topic, string connectionString)
        {
            var client = new ServiceBusAdministrationClient(connectionString);
            var subscriptionName = "order-subscription";

            Response<bool> subscriptionExists = await client.SubscriptionExistsAsync(topic, subscriptionName);
            if (!subscriptionExists.Value)
            {
                await client.CreateSubscriptionAsync(topic, subscriptionName);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            var exceptions = new Collection<Exception>();

            try
            {
                Dispose();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }

            try
            {
                await MessagePump.DisposeAsync();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }

            if (exceptions.Count is 1)
            {
                throw exceptions[0];
            }

            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected override void Disposing(bool disposing)
        {
            base.Disposing(disposing);
            Environment.SetEnvironmentVariable("ServiceBusConnectionString", null);
            Environment.SetEnvironmentVariable("EVENTGRID_TOPIC_URI", null);
            Environment.SetEnvironmentVariable("EVENTGRID_AUTH_KEY", null);
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
        }
    }
}
