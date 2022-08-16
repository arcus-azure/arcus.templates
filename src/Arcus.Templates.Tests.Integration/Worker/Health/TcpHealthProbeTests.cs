using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Health
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class TcpHealthProbeTests
    {
        private readonly ITestOutputHelper _outputWriter;

        public TcpHealthProbeTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(ServiceBusEntityType.Queue)]
        [InlineData(ServiceBusEntityType.Topic)]
        public async Task MinimumWorker_ProbeForHealthReport_ResponseHealthy(ServiceBusEntityType entityType)
        {
            // Arrange
            await using (var project = await WorkerMessagingProject.StartNewWithServiceBusAsync(entityType, _outputWriter))
            {
                // Act
                HealthStatus status = await project.Health.ProbeHealthAsync();

                // Assert
                Assert.Equal(HealthStatus.Healthy, status);
            }
        }
    }
}
