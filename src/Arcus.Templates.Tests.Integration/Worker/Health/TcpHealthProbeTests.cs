using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
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
            using (var project = await ServiceBusQueueWorkerProject.StartNewAsync(_outputWriter))
            {
                project.TearDownOptions = TearDownOptions.KeepProjectDirectory;

                // Act
                HealthReport report = await project.Health.ProbeHealthReportAsync();

                // Assert
                Assert.Equal(HealthStatus.Healthy, report.Status);
            }
        }
    }
}
