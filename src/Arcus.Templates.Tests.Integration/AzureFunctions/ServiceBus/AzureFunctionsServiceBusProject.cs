using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus.MessageHandling;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using GuardNet;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Project template to create new Azure Functions Service Bus projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}")]
    public class AzureFunctionsServiceBusProject : AzureFunctionsProject, IAsyncDisposable
    {
        private readonly ServiceBusEntityType _entityType;

        private AzureFunctionsServiceBusProject(
            ServiceBusEntityType entityType, 
            TestConfig configuration, 
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsServiceBusProjectDirectory(entityType), 
                   configuration, 
                   outputWriter)
        {
            string connectionString = configuration.GetServiceBusConnectionString(entityType);
            var producer = new TestServiceBusMessageProducer(connectionString);
            MessagePump = new MessagePumpService(producer, configuration, outputWriter);
        }

        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the Azure Functions Service Bus project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public MessagePumpService MessagePump { get; }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <param name="entityType">The type of the Azure Service Bus entity, to control the used project template.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions Service Bus Topic project with a set of services to interact with the worker.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static async Task<AzureFunctionsServiceBusProject> StartNewProjectAsync(
            ServiceBusEntityType entityType,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            return await StartNewProjectAsync(entityType, new AzureFunctionsServiceBusProjectOptions(entityType), configuration, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <param name="entityType">The type of the Azure Service Bus entity, to control the used project template.</param>
        /// <param name="options">The additional project options to pass along to the project creation command.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions Service Bus Topic project with a set of services to interact with the project.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static async Task<AzureFunctionsServiceBusProject> StartNewProjectAsync(
            ServiceBusEntityType entityType,
            AzureFunctionsServiceBusProjectOptions options,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of project options to pass along to the project creation command");
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsServiceBusProject project = CreateNew(entityType, options, configuration, outputWriter);

            await project.StartAsync(entityType);
            return project;
        }

        private static AzureFunctionsServiceBusProject CreateNew(
            ServiceBusEntityType entityType, 
            AzureFunctionsServiceBusProjectOptions options, 
            TestConfig configuration, 
            ITestOutputHelper outputWriter)
        {
            var project = new AzureFunctionsServiceBusProject(entityType, configuration, outputWriter);
            project.CreateNewProject(options);
            project.AddOrderMessageHandlerImplementation(options);
            project.AddLocalSettings(options.FunctionsWorker);

            return project;
        }

        private void AddOrderMessageHandlerImplementation(AzureFunctionsServiceBusProjectOptions options)
        {
            AddPackage("Arcus.EventGrid", "3.2.0");
            AddPackage("Arcus.EventGrid.Publishing", "3.2.0");

            AddTypeAsFile<Order>();
            AddTypeAsFile<Customer>();
            AddTypeAsFile<OrderCreatedEvent>();
            AddTypeAsFile<OrderCreatedEventData>();

            if (options.FunctionsWorker is FunctionsWorker.InProcess)
            {
                AddTypeAsFile<TestOrdersAzureServiceBusMessageHandler>();
                UpdateFileInProject("Startup.cs", contents =>
                    RemovesUserErrorsFromContents(contents)
                        .Replace("OrdersAzureServiceBusMessageHandler", nameof(TestOrdersAzureServiceBusMessageHandler))); 
            } 
            else if (options.FunctionsWorker is FunctionsWorker.Isolated)
            {
                AddTypeAsFile<TestOrdersAzureServiceBusMessageHandler>();
                UpdateFileInProject("Program.cs", contents =>
                    RemovesUserErrorsFromContents(contents)
                        .Replace("OrdersAzureServiceBusMessageHandler", nameof(TestOrdersAzureServiceBusMessageHandler)));
            }
        }

        private async Task StartAsync(ServiceBusEntityType entityType)
        {
            string serviceBusConnectionString = Configuration.GetServiceBusConnectionString(entityType);
            var properties = ServiceBusConnectionStringProperties.Parse(serviceBusConnectionString);
            string namespaceConnectionString = $"Endpoint={properties.Endpoint};SharedAccessKeyName={properties.SharedAccessKeyName};SharedAccessKey={properties.SharedAccessKey}";
            Environment.SetEnvironmentVariable("ServiceBusConnectionString", namespaceConnectionString);

            if (entityType is ServiceBusEntityType.Topic)
            {
                await AddServiceBusTopicSubscriptionAsync(properties.EntityPath, namespaceConnectionString);
            }

            EventGridConfig eventGridConfig = Configuration.GetEventGridConfig();
            Environment.SetEnvironmentVariable("EVENTGRID_TOPIC_URI", eventGridConfig.TopicUri);
            Environment.SetEnvironmentVariable("EVENTGRID_AUTH_KEY", eventGridConfig.AuthenticationKey);

            string instrumentationKey = Configuration.GetApplicationInsightsInstrumentationKey();
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", instrumentationKey);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", $"InstrumentationKey={instrumentationKey}");

            Run(Configuration.BuildConfiguration, TargetFramework.Net6_0);
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
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);
        }
    }
}
