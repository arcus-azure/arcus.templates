using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Project template to create new Azure Functions Service Bus projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}")]
    public class AzureFunctionsServiceBusProject : AzureFunctionsProject, IAsyncDisposable
    {
        private const string AzureServiceBusConnectionString = "Arcus:ServiceBus:ConnectionString",
                             AzureServiceBusQueueName = "QueueName";
        
        private readonly ServiceBusEntity _entity;
        
        public AzureFunctionsServiceBusProject(
            ServiceBusEntity entity,
            TestConfig configuration, 
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsServiceBusDirectory(entity), 
                   configuration, 
                   outputWriter)
        {
            _entity = entity;
            
            MessagePump = new MessagePumpService(entity, configuration, outputWriter);
        }
        
        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the Service Bus worker project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public MessagePumpService MessagePump { get; }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A Azure Functions Service Bus project with a full set of endpoint services to interact with the Azure Function.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the Azure Functions project cannot be started correctly.</exception>
        public static async Task<AzureFunctionsServiceBusProject> StartNewWithQueueAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to create a Azure Functions Service Bus Queue project");
            
            AzureFunctionsServiceBusProject project = await StartNewWithQueueAsync(TestConfig.Create(), outputWriter);
            return project;
        }
        
        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A Azure Functions Service Bus project with a full set of endpoint services to interact with the Azure Function.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the Azure Functions project cannot be started correctly.</exception>
        public static async Task<AzureFunctionsServiceBusProject> StartNewWithQueueAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to create a Azure Functions Service Bus Queue project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to create a Azure Functions Service Bus Queue project");
            
            AzureFunctionsServiceBusProject project = CreateNew(ServiceBusEntity.Queue, configuration, outputWriter);
            await project.StartAsync();
            await project.MessagePump.StartAsync();

            return project;
        }

        private static AzureFunctionsServiceBusProject CreateNew(ServiceBusEntity entity, TestConfig configuration, ITestOutputHelper outputWriter)
        {
            var project = new AzureFunctionsServiceBusProject(entity, configuration, outputWriter);
            project.CreateNewProject(new ProjectOptions());
            project.AddStorageAccount();
            project.AddOrdersMessageHandling();
            
            return project;
        }

        private void AddOrdersMessageHandling()
        {
            AddPackage("Arcus.EventGrid", "3.0.0");
            AddPackage("Arcus.EventGrid.Publishing", "3.0.0");
            AddTypeAsFile<Order>();
            AddTypeAsFile<OrderCreatedEvent>();
            AddTypeAsFile<OrderCreatedEventData>();
            AddTypeAsFile<OrdersMessageHandler>();

            UpdateFileInProject("Startup.cs", contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace("EmptyMessageHandler", nameof(OrdersMessageHandler))
                    .Replace("EmptyMessage", nameof(Order)));
        }

        private async Task StartAsync()
        {
            string serviceBusConnectionString = Configuration.GetServiceBusConnectionString(_entity);
            var serviceBusConnection = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
            
            Environment.SetEnvironmentVariable(AzureServiceBusConnectionString, serviceBusConnection.GetNamespaceConnectionString());
            Environment.SetEnvironmentVariable(AzureServiceBusQueueName, serviceBusConnection.EntityPath);
            
            Run(BuildConfiguration.Debug, TargetFramework.NetCoreApp31);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            Environment.SetEnvironmentVariable(AzureServiceBusConnectionString, null);
            Environment.SetEnvironmentVariable(AzureServiceBusQueueName, null);
            
            Dispose();

            await MessagePump.DisposeAsync();
        }
    }
}
