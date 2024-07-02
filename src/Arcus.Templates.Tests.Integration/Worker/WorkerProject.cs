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
using Polly;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker
{
    public class WorkerProject : TemplateProject, IAsyncDisposable
    {
        private readonly int _healthPort;
        private readonly TestConfig _configuration;

        protected WorkerProject(
            DirectoryInfo templateDirectory,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
            : base(templateDirectory, configuration.GetFixtureProjectDirectory(), outputWriter)
        {
            _healthPort = configuration.GenerateWorkerHealthPort();
            _configuration = configuration;

            Health = new HealthEndpointService(_healthPort, outputWriter);
        }

        protected WorkerProject(
            DirectoryInfo templateDirectory,
            TestConfig configuration,
            IMessagingService messaging,
            ITestOutputHelper outputWriter)
            : this(templateDirectory, configuration, outputWriter)
        {
            Messaging = messaging;
        }
        /// <summary>
        /// Gets the service that interacts with the exposed health report information of the worker project.
        /// </summary>
        public HealthEndpointService Health { get; }

        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the worker project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public IMessagingService Messaging { get; protected init; }

        /// <summary>
        /// Starts a new .NET Worker project from a project template.
        /// </summary>
        /// <param name="options">The user-defined options to manipulate the contents of the project created from the project template.</param>
        /// <param name="additionalArguments">The additional CLI arguments passed along the startup command when starting the project created from the project template.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        protected async Task StartAsync(WorkerProjectOptions options, params CommandArgument[] additionalArguments)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of user-defined options to manipulate the contents of the .NET Worker project created from the project template");
            
            CommandArgument[] commands = 
                CreateDefaultWorkerCommand()
                    .Concat(options.AdditionalRunArguments)
                    .Concat(additionalArguments)
                    .ToArray();

            try
            {
                Run(_configuration.BuildConfiguration, TargetFramework.Net8_0, commands);
                await WaitUntilWorkerProjectIsAvailableAsync(_healthPort);
            }
            catch
            {
                await DisposeAsync();
                throw;
            }
        }

        private IEnumerable<CommandArgument> CreateDefaultWorkerCommand()
        {
            return new[]
            {
                CommandArgument.CreateOpen("ARCUS_HEALTH_PORT", _healthPort),
            };
        }

        private async Task WaitUntilWorkerProjectIsAvailableAsync(int tcpPort)
        {
            IAsyncPolicy waitAndRetryForeverAsync =
                Policy.Handle<Exception>()
                      .WaitAndRetryForeverAsync(retryNumber => TimeSpan.FromSeconds(1));

            PolicyResult result = 
                await Policy.TimeoutAsync(TimeSpan.FromSeconds(15))
                            .WrapAsync(waitAndRetryForeverAsync)
                            .ExecuteAndCaptureAsync(() => TryToConnectToTcpListener(tcpPort));

            if (result.Outcome == OutcomeType.Successful)
            {
                Logger.WriteLine("Test template worker project fully started at: localhost:{0}", tcpPort);
            }
            else
            {
                Logger.WriteLine("Test template worker project could not be started");
                throw new CannotStartTemplateProjectException(
                    "The test project created from the worker project template doesn't seem to be running, "
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

            await DisposingAsync(true);
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected virtual ValueTask DisposingAsync(bool disposing)
        {
            return ValueTask.CompletedTask;
        }
    }
}
