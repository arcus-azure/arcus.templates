using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

        [Fact]
        public async Task MinimumServiceBusQueueWorker_ProbeForHealthReport_ResponseHealthy()
        {
            // Arrange
            await using (var project = await ServiceBusWorkerProject.StartNewWithQueueAsync(_outputWriter))
            {
                // Act
                HealthReport report = await project.Health.ProbeHealthReportAsync();

                // Assert
                Assert.Equal(HealthStatus.Healthy, report.Status);
            }
        }
    }
}
