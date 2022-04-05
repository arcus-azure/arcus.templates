using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using Azure.Messaging.ServiceBus;
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

            AzureFunctionsServiceBusProject project = CreateNew(ServiceBusEntity.Queue, configuration, outputWriter);
            
            await project.StartAsync(ServiceBusEntity.Queue);
            return project;
        }

        private static AzureFunctionsServiceBusProject CreateNew(ServiceBusEntity entity, TestConfig configuration, ITestOutputHelper outputWriter)
        {
            var project = new AzureFunctionsServiceBusProject(entity, configuration, outputWriter);
            project.CreateNewProject(new ProjectOptions());
            project.AddOrderMessageHandlerImplementation();
            project.AddStorageAccount();

            return project;
        }

        private void AddOrderMessageHandlerImplementation()
        {
            AddPackage("Arcus.EventGrid", "3.1.0");
            AddPackage("Arcus.EventGrid.Publishing", "3.1.0");

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

            EventGridConfig eventGridConfig = Configuration.GetEventGridConfig();
            Environment.SetEnvironmentVariable("EVENTGRID_TOPIC_URI", eventGridConfig.TopicUri);
            Environment.SetEnvironmentVariable("EVENTGRID_AUTH_KEY", eventGridConfig.AuthenticationKey);

            Run(BuildConfiguration.Release, TargetFramework.Net6_0);
            await MessagePump.StartAsync();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            Dispose();
            await MessagePump.DisposeAsync();
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
        }
    }
}
