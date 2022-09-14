using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Health
{
    [Collection(TestCollections.Docker)]
    [Trait("Category", TestTraits.Docker)]
    public class TcpHealthDockerProbeTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthDockerProbeTests"/> class.
        /// </summary>
        public TcpHealthDockerProbeTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task MinimumServiceBusQueueWorkerOnDocker_ProbeForHealthReport_ResponseHealthy()
        {
            // Arrange
            var configuration = TestConfig.Create();
            int healthPort = configuration.GetDockerServiceBusQueueWorkerHealthPort();

            var healthEndpointService = new HealthEndpointService(healthPort, _outputWriter);

            // Act
            HealthStatus status = await healthEndpointService.ProbeHealthAsync();

            // Assert
            Assert.Equal(HealthStatus.Healthy, status);
        }

        [Fact]
        public async Task MinimumServiceBusTopicWorkerOnDocker_ProbeForHealthReport_ResponseHealthy()
        {
            // Arrange
            var configuration = TestConfig.Create();
            int healthPort = configuration.GetDockerServiceBusTopicWorkerHealthPort();

            var healthEndpointService = new HealthEndpointService(healthPort, _outputWriter);

            // Act
            HealthStatus status = await healthEndpointService.ProbeHealthAsync();

            // Assert
            Assert.Equal(HealthStatus.Healthy, status);
        }

        [Fact]
        public async Task EventHubsWorkerOnDocker_ProbeForHealthReport_ResponseHealthy()
        {
            // Arrange
            var configuration = TestConfig.Create();
            int healthPort = configuration.GetDockerEventHubsWorkerHealthPort();

            var healthEndpointService = new HealthEndpointService(healthPort, _outputWriter);

            // Act
            HealthStatus status = await healthEndpointService.ProbeHealthAsync();

            // Assert
            Assert.Equal(HealthStatus.Healthy, status);
        }
    }
}
