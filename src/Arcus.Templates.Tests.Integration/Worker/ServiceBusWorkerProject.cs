using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Health;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using GuardNet;
using Polly;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Project template to create Azure ServiceBus Queue worker projects.
    /// </summary>
    public class ServiceBusWorkerProject : TemplateProject, IAsyncDisposable
    {
        private readonly ServiceBusEntity _entity;
        private readonly int _healthPort;
        private readonly TestConfig _configuration;

        private ServiceBusWorkerProject(
            ServiceBusEntity entity,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
            : base(configuration.GetServiceBusProjectDirectory(entity), 
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
        public static async Task<ServiceBusWorkerProject> StartNewWithQueueAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter));

            var config = TestConfig.Create();
            var options = ServiceBusWorkerProjectOptions.Create(config);
            ServiceBusWorkerProject project = await StartNewWithQueueAsync(config, options, outputWriter);
            
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="config">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithQueueAsync(
            TestConfig config,
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            ServiceBusWorkerProject project = await StartNewAsync(ServiceBusEntity.Queue, config, options, outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithTopicAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter));

            var config = TestConfig.Create();
            var options = ServiceBusWorkerProjectOptions.Create(config);
            ServiceBusWorkerProject project = await StartNewWithTopicAsync(config, options, outputWriter);
            
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="config">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithTopicAsync(
            TestConfig config,
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            ServiceBusWorkerProject project = await StartNewAsync(ServiceBusEntity.Topic, config, options, outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue or Topic worker project template.
        /// </summary>
        /// <param name="entity">The resource entity for which the worker template should be created, you can also use <see cref="StartNewWithQueueAsync(ITestOutputHelper)"/> or <see cref="StartNewWithTopicAsync(ITestOutputHelper)"/> instead.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewAsync(
            ServiceBusEntity entity, 
            TestConfig configuration, 
            ServiceBusWorkerProjectOptions options, 
            ITestOutputHelper outputWriter)
        {
            ServiceBusWorkerProject project = CreateNew(entity, configuration, options, outputWriter);
            await project.StartAsync(options);
            await project.MessagePump.StartAsync();

            return project;
        }

        private static ServiceBusWorkerProject CreateNew(
            ServiceBusEntity entity, 
            TestConfig configuration, 
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            var project = new ServiceBusWorkerProject(entity, configuration, outputWriter);
            project.CreateNewProject(options);
            project.AddOrdersMessagePump();

            return project;
        }

        private void AddOrdersMessagePump()
        {
            AddPackage("Arcus.EventGrid", "3.0.0");
            AddPackage("Arcus.EventGrid.Publishing", "3.0.0");
            AddTypeAsFile<Order>();
            AddTypeAsFile<OrderCreatedEvent>();
            AddTypeAsFile<OrderCreatedEventData>();
            AddTypeAsFile<OrdersMessageHandler>();
            AddTypeAsFile<SingleValueSecretProvider>();

            string connectionString = _configuration.GetServiceBusConnectionString(_entity);
            UpdateFileInProject("Program.cs", contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace("EmptyMessageHandler", nameof(OrdersMessageHandler))
                    .Replace("EmptyMessage", nameof(Order))
                    .Replace("AddAzureKeyVaultWithManagedServiceIdentity(\"https://your-keyvault-vault.azure.net/\")", 
                             $"AddProvider(new {nameof(SingleValueSecretProvider)}(\"{connectionString}\"))"));
        }

        private async Task StartAsync(ServiceBusWorkerProjectOptions options)
        {
            CommandArgument[] commands = 
                CreateServiceBusQueueWorkerCommands()
                    .Concat(options.AdditionalArguments)
                    .ToArray();
            
            Run(_configuration.BuildConfiguration, _configuration.TargetFramework, commands);
            await WaitUntilWorkerProjectIsAvailableAsync(_healthPort);
        }

        private IEnumerable<CommandArgument> CreateServiceBusQueueWorkerCommands()
        {
            string eventGridTopicUri = _configuration["Arcus:Worker:EventGrid:TopicUri"];
            string eventGridAuthKey = _configuration["Arcus:Worker:EventGrid:AuthKey"];
            string serviceBusConnection = _configuration.GetServiceBusConnectionString(_entity);

            return new[]
            {
                CommandArgument.CreateOpen("ARCUS_HEALTH_PORT", _healthPort),
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", eventGridTopicUri),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", eventGridAuthKey),
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
