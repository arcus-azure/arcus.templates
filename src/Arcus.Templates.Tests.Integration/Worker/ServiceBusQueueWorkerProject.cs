using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
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
    public class ServiceBusQueueWorkerProject : TemplateProject, IAsyncDisposable
    {
        private readonly int _healthPort;
        private readonly TestConfig _configuration;

        private ServiceBusQueueWorkerProject(
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
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusQueueWorkerProject> StartNewAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter));

            ServiceBusQueueWorkerProject project = await StartNewAsync(TestConfig.Create(), outputWriter);
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
        public static async Task<ServiceBusQueueWorkerProject> StartNewAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            ServiceBusQueueWorkerProject project = CreateNew(configuration, outputWriter);

            await project.StartAsync();
            var connectionString = configuration.GetValue<string>("Arcus:Worker:ServiceBus:ConnectionString");
            var topicName = configuration.GetValue<string>("Arcus:Worker:ServiceBus:TopicName");

            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);
            var serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, new XunitTestLogger(outputWriter));
            project.MessagePump = new MessagePumpService(configuration, serviceBusEventConsumerHost);

            return project;
        }

        private static ServiceBusQueueWorkerProject CreateNew(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            DirectoryInfo templateDirectory = configuration.GetServiceBusQueueProjectDirectory();
            DirectoryInfo fixtureDirectory = configuration.GetFixtureProjectDirectory();
            int healthPort = configuration.GenerateWorkerHealthPort();

            var project = new ServiceBusQueueWorkerProject(healthPort, configuration, templateDirectory, fixtureDirectory, outputWriter);
            project.CreateNewProject(new ProjectOptions());
            project.AddOrdersMessagePump();

            return project;
        }

        private void AddOrdersMessagePump()
        {
            AddPackage("Arcus.EventGrid", "3.0.0-preview-1");
            AddPackage("Arcus.EventGrid.Publishing", "3.0.0-preview-1");
            AddTypeAsFile<Order>();
            AddTypeAsFile<OrderCreatedEvent>();
            AddTypeAsFile<OrderCreatedEventData>();
            AddTypeAsFile<OrdersMessagePump>();
            AddTypeAsFile<SingleValueSecretProvider>();

            var connectionStringWithQueue = _configuration.GetValue<string>("Arcus:Worker:ServiceBus:ConnectionStringWithQueue");
            UpdateFileInProject("Program.cs", contents => 
                RemoveCustomUserErrors(contents)
                    .Replace("EmptyMessagePump", nameof(OrdersMessagePump))
                    .Replace("secretProvider: null", $"new {nameof(SingleValueSecretProvider)}(\"{connectionStringWithQueue}\")"));
        }

        private static string RemoveCustomUserErrors(string content)
        {
            return content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                          .Where(line => !line.Contains("#error"))
                          .Aggregate((line1, line2) => line1 + Environment.NewLine + line2);
        }

        private async Task StartAsync()
        {
            var commands = CreateServiceBusQueueWorkerCommands(_configuration, _healthPort);
            Run(_configuration.BuildConfiguration, TargetFramework.NetCoreApp30, commands.ToArray());
            await WaitUntilWorkerProjectIsAvailableAsync(_healthPort);
        }

        private static IEnumerable<CommandArgument> CreateServiceBusQueueWorkerCommands(IConfiguration configuration, int healthPort)
        {
            string eventGridTopicUri = configuration["Arcus:Worker:EventGrid:TopicUri"];
            string eventGridAuthKey = configuration["Arcus:Worker:EventGrid:AuthKey"];
            string serviceBusQueueConnection = configuration["Arcus:Worker:ServiceBus:ConnectionStringWithQueue"];

            return new[]
            {
                CommandArgument.CreateOpen("ARCUS_HEALTH_PORT", healthPort),
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", eventGridTopicUri),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", eventGridAuthKey),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", serviceBusQueueConnection)
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
                Logger.WriteLine("Test template ServiceBus Queue worker project fully started at: localhost:{0}", tcpPort);
            }
            else
            {
                Logger.WriteLine("Test template ServiceBus Queue project could not be started");
                throw new CannotStartTemplateProjectException(
                    "The test project created from the ServiceBus Queue project template doesn't seem to be running, "
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
        /// Gets the service that interacts with the exposed health report information of the ServiceBus Queue worker project.
        /// </summary>
        public HealthEndpointService Health { get; }

        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the ServiceBus Queue worker project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public MessagePumpService MessagePump { get; private set; }

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
