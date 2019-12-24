using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
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
        private readonly int _tcpPort;

        private ServiceBusQueueWorkerProject(
            int tcpPort,
            DirectoryInfo templateDirectory,
            DirectoryInfo fixtureDirectory,
            ITestOutputHelper outputWriter)
            : base(templateDirectory, fixtureDirectory, outputWriter)
        {
            _tcpPort = tcpPort;

            Health = new HealthEndpointService(_tcpPort, outputWriter);
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

            var configuration = TestConfig.Create();

            DirectoryInfo templateDirectory = configuration.GetServiceBusQueueProjectDirectory();
            DirectoryInfo fixtureDirectory = configuration.GetFixtureProjectDirectory();
            int healthPort = configuration.GenerateWorkerHealthPort();

            var project = new ServiceBusQueueWorkerProject(healthPort, templateDirectory, fixtureDirectory, outputWriter);
            project.CreateNewProject(new ProjectOptions());

            string commands = CreateServiceBusQueueWorkerCommands(configuration, healthPort);
            project.Run(configuration.BuildConfiguration, TargetFramework.NetCoreApp30, commands);
            await project.WaitUntilWorkerProjectIsAvailableAsync(healthPort);

            return project;
        }

        private static string CreateServiceBusQueueWorkerCommands(IConfiguration configuration, int healthPort)
        {
            string eventGridTopicUri = configuration["Arcus_Worker_EventGrid_TopicUri"];
            string eventGridAuthKey = configuration["Arcus_Worker_EventGrid_AuthKey"];
            string serviceBusQueueConnection = configuration["Arcus_Worker_ServiceBus_ConnectionStringWithQueue"];
            
            return $"--ARCUS_HEALTH_PORT {healthPort} "
                   + $"--EVENTGRID_TOPIC_URI {eventGridTopicUri} "
                   + $"--EVENTGRID_AUTH_KEY {eventGridAuthKey} "
                   + $"--ARCUS_SERVICEBUS_CONNECTIONSTRING {serviceBusQueueConnection}";
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
            return $"Project: {ProjectDirectory.FullName}, running at: localhost:{_tcpPort}";
        }
    }
}
