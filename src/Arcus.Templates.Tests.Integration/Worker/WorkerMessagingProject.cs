using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Health;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Project template to create Azure ServiceBus Queue worker projects.
    /// </summary>
    public class WorkerMessagingProject : TemplateProject, IAsyncDisposable
    {
        private readonly ServiceBusEntityType _entity;
        private readonly int _healthPort;
        private readonly TestConfig _configuration;

        private WorkerMessagingProject(
            ServiceBusEntityType entity,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
            : base(configuration.GetWorkerMessagingProjectDirectory(), 
                   configuration.GetFixtureProjectDirectory(), 
                   outputWriter)
        {
            _entity = entity;
            _healthPort = configuration.GenerateWorkerHealthPort();
            _configuration = configuration;

            Health = new HealthEndpointService(_healthPort, outputWriter);
            MessagePump = new MessagePumpService(entity, configuration, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<WorkerMessagingProject> StartNewWithServiceBusQueueAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter));

            var config = TestConfig.Create();
            var options = WorkerMessagingProjectOptions.CreateForServiceBusQueue(config);
            WorkerMessagingProject project = await StartNewAsync(config, options, outputWriter);
            
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<WorkerMessagingProject> StartNewWithServiceBusTopicAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter));

            var config = TestConfig.Create();
            var options = WorkerMessagingProjectOptions.CreateForServiceBusTopic(config);
            WorkerMessagingProject project = await StartNewAsync(config, options, outputWriter);
            
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue or Topic worker project template.
        /// </summary>
        /// <param name="entityType">The type of the Azure Service Bus entity that should be used as messaging source in the worker messaging project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<WorkerMessagingProject> StartNewWithServiceBusAsync(
            ServiceBusEntityType entityType, 
            ITestOutputHelper outputWriter)
        {
            var config = TestConfig.Create();
            var options = WorkerMessagingProjectOptions.CreateForServiceBus(entityType, config);
            
            return await StartNewAsync(config, options, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue or Topic worker project template.
        /// </summary>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<WorkerMessagingProject> StartNewAsync(
            WorkerMessagingProjectOptions options, 
            ITestOutputHelper outputWriter)
        {
            return await StartNewAsync(TestConfig.Create(), options, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue or Topic worker project template.
        /// </summary>
        /// <param name="entity">The resource entity for which the worker template should be created, you can also use <see cref="StartNewWithServiceBusQueueAsync"/> or <see cref="StartNewWithServiceBusTopicAsync(Xunit.Abstractions.ITestOutputHelper)"/> instead.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<WorkerMessagingProject> StartNewAsync(
            TestConfig configuration, 
            WorkerMessagingProjectOptions options, 
            ITestOutputHelper outputWriter)
        {
            WorkerMessagingProject project = CreateNew(options.ServiceBusEntityType, configuration, options, outputWriter);
            await project.StartAsync(options);
            await project.MessagePump.StartAsync();

            return project;
        }

        private static WorkerMessagingProject CreateNew(
            ServiceBusEntityType entity, 
            TestConfig configuration, 
            WorkerMessagingProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            var project = new WorkerMessagingProject(entity, configuration, outputWriter);
            project.CreateNewProject(options);
            project.AddOrdersMessagePump();

            return project;
        }

        private void AddOrdersMessagePump()
        {
            AddPackage("Arcus.EventGrid", "3.2.0");
            AddPackage("Arcus.EventGrid.Publishing", "3.2.0");
            AddTypeAsFile<Order>();
            AddTypeAsFile<Customer>();
            AddTypeAsFile<OrderCreatedEvent>();
            AddTypeAsFile<OrderCreatedEventData>();
            AddTypeAsFile<OrdersMessageHandler>();
            
            UpdateFileInProject("Program.cs", contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace(".MinimumLevel.Debug()", ".MinimumLevel.Verbose()")
                    .Replace("EmptyMessageHandler", nameof(OrdersMessageHandler))
                    .Replace("EmptyMessage", nameof(Order))
                    .Replace("stores.AddAzureKeyVaultWithManagedIdentity(\"https://your-keyvault.vault.azure.net/\", CacheConfiguration.Default);", ""));
        }

        private async Task StartAsync(WorkerMessagingProjectOptions options)
        {
            CommandArgument[] commands = 
                CreateServiceBusQueueWorkerCommands()
                    .Concat(options.AdditionalArguments)
                    .ToArray();
            
            Run(_configuration.BuildConfiguration, TargetFramework.Net6_0, commands);
            await WaitUntilWorkerProjectIsAvailableAsync(_healthPort);
        }

        private IEnumerable<CommandArgument> CreateServiceBusQueueWorkerCommands()
        {
            EventGridConfig eventGridConfig = _configuration.GetEventGridConfig();
            string serviceBusConnection = _configuration.GetServiceBusConnectionString(_entity);

            return new[]
            {
                CommandArgument.CreateOpen("ARCUS_HEALTH_PORT", _healthPort),
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", eventGridConfig.TopicUri),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", eventGridConfig.AuthenticationKey),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", serviceBusConnection)
            };
        }

        private async Task WaitUntilWorkerProjectIsAvailableAsync(int tcpPort)
        {
            IAsyncPolicy waitAndRetryForeverAsync =
                Policy.Handle<Exception>()
                      .WaitAndRetryForeverAsync(retryNumber => TimeSpan.FromSeconds(1));

            PolicyResult result = 
                await Policy.TimeoutAsync(TimeSpan.FromSeconds(10))
                            .WrapAsync(waitAndRetryForeverAsync)
                            .ExecuteAndCaptureAsync(() => TryToConnectToTcpListener(tcpPort));

            if (result.Outcome == OutcomeType.Successful)
            {
                Logger.WriteLine("Test template Service Bus worker project fully started at: localhost:{0}", tcpPort);
            }
            else
            {
                Logger.WriteLine("Test template Service Bus project could not be started");
                throw new CannotStartTemplateProjectException(
                    "The test project created from the Service Bus project template doesn't seem to be running, "
                    + "please check any build or runtime errors that could occur when the test project was created");
            }
        }

        private static async Task TryToConnectToTcpListener(int tcpPort)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), tcpPort);
                client.Close();
            }
        }

        /// <summary>
        /// Gets the service that interacts with the exposed health report information of the Service Bus worker project.
        /// </summary>
        public HealthEndpointService Health { get; }

        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the Service Bus worker project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public MessagePumpService MessagePump { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"Project: {ProjectDirectory.FullName}, running at: localhost:{_healthPort}";
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
    }
}
