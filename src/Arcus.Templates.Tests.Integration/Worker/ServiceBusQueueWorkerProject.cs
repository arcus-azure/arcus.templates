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
using GuardNet;
using Microsoft.Extensions.Configuration;
using Polly;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Project template to create Azure ServiceBus Queue worker projects.
    /// </summary>
    public class ServiceBusQueueWorkerProject : TemplateProject
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
        /// Creates a new Azure ServiceBus Queue worker project from the project template.
        /// </summary>
        /// <param name="configuration">The configuration used when configuring the message pump of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A not yet started Azure ServiceBus Queue worker project with a full set of services to interact with the worker.
        /// </returns>
        /// <remarks>
        ///     Before the project can be interacted with, the project must be started by calling the <see cref="StartAsync" /> method.
        /// </remarks>
        public static ServiceBusQueueWorkerProject CreateNew(TestConfig configuration, ITestOutputHelper outputWriter)
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
            return project;
        }

        /// <summary>
        /// Starts the Azure ServiceBus Queue worker project; created from the template.
        /// </summary>
        public async Task StartAsync()
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
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"Project: {ProjectDirectory.FullName}, running at: localhost:{_healthPort}";
        }
    }
}
