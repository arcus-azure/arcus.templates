using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Health;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Polly;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Project template to create Azure ServiceBus Queue worker projects.
    /// </summary>
    public class ServiceBusWorkerProject : TemplateProject, IAsyncDisposable
    {
        private readonly int _healthPort;
        private readonly TestConfig _configuration;

        private ServiceBusWorkerProject(
            int healthPort,
            TestConfig configuration,
            DirectoryInfo templateDirectory,
            DirectoryInfo fixtureDirectory,
            ITestOutputHelper outputWriter)
            : base(templateDirectory, fixtureDirectory, outputWriter)
        {
            _healthPort = healthPort;
            _configuration = configuration;

            Health = new HealthEndpointService(_healthPort, outputWriter);
            MessagePump = new MessagePumpService(configuration, outputWriter);
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

            ServiceBusWorkerProject project = await StartNewWithQueueAsync(TestConfig.Create(), outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="configuration">The configuration used to retrieve information to make the project runnable (i.e. connection strings for the message pump).</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithQueueAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            const string connectionStringConfigurationKey = "Arcus:Worker:ServiceBus:ConnectionStringWithQueue";
            DirectoryInfo templateDirectory = configuration.GetServiceBusQueueProjectDirectory();

            ServiceBusWorkerProject project = CreateNew(templateDirectory, connectionStringConfigurationKey, configuration, outputWriter);
            await project.StartAsync(connectionStringConfigurationKey);
            await project.MessagePump.StartAsync();

            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="configuration">The configuration used to retrieve information to make the project runnable (i.e. connection strings for the message pump).</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithTopicAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            const string connectionStringConfigurationKey = "Arcus:Worker:ServiceBus:ConnectionStringWithTopic";
            DirectoryInfo templateDirectory = configuration.GetServiceBusTopicProjectDirectory();

            ServiceBusWorkerProject project = CreateNew(templateDirectory, connectionStringConfigurationKey, configuration, outputWriter);
            await project.StartAsync(connectionStringConfigurationKey);
            await project.MessagePump.StartAsync();

            return project;
        }

        private static ServiceBusWorkerProject CreateNew(
            DirectoryInfo templateDirectory,
            string connectionStringConfigurationKey,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
        {
            DirectoryInfo fixtureDirectory = configuration.GetFixtureProjectDirectory();
            int healthPort = configuration.GenerateWorkerHealthPort();

            var project = new ServiceBusWorkerProject(healthPort, configuration, templateDirectory, fixtureDirectory, outputWriter);
            project.CreateNewProject(new ProjectOptions());
            project.AddOrdersMessagePump(connectionStringConfigurationKey: connectionStringConfigurationKey);

            return project;
        }

        private void AddOrdersMessagePump(string connectionStringConfigurationKey)
        {
            AddPackage("Arcus.EventGrid", "3.0.0-preview-1");
            AddPackage("Arcus.EventGrid.Publishing", "3.0.0-preview-1");
            AddTypeAsFile<Order>();
            AddTypeAsFile<OrderCreatedEvent>();
            AddTypeAsFile<OrderCreatedEventData>();
            AddTypeAsFile<OrdersMessagePump>();
            AddTypeAsFile<SingleValueSecretProvider>();

            var connectionString = _configuration.GetValue<string>(connectionStringConfigurationKey);
            UpdateFileInProject("Program.cs", contents => 
                RemoveCustomUserErrors(contents)
                    .Replace("EmptyMessagePump", nameof(OrdersMessagePump))
                    .Replace("secretProvider: null", $"new {nameof(SingleValueSecretProvider)}(\"{connectionString}\")"));
        }

        private static string RemoveCustomUserErrors(string content)
        {
            return content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                          .Where(line => !line.Contains("#error"))
                          .Aggregate((line1, line2) => line1 + Environment.NewLine + line2);
        }


        private async Task StartAsync(string connectionStringConfigurationKey)
        {
            IEnumerable<CommandArgument> commands = CreateServiceBusQueueWorkerCommands(connectionStringConfigurationKey, _configuration, _healthPort);
            Run(_configuration.BuildConfiguration, TargetFramework.NetCoreApp30, commands.ToArray());
            await WaitUntilWorkerProjectIsAvailableAsync(_healthPort);
        }

        private static IEnumerable<CommandArgument> CreateServiceBusQueueWorkerCommands(
            string connectionStringConfigurationKey,
            IConfiguration configuration,
            int healthPort)
        {
            string eventGridTopicUri = configuration["Arcus:Worker:EventGrid:TopicUri"];
            string eventGridAuthKey = configuration["Arcus:Worker:EventGrid:AuthKey"];
            string serviceBusConnection = configuration[connectionStringConfigurationKey];

            return new[]
            {
                CommandArgument.CreateOpen("ARCUS_HEALTH_PORT", healthPort),
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
