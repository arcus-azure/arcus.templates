using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Logging
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class ExcludeSerilogOptionTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeSerilogOptionTests"/> class.
        /// </summary>
        public ExcludeSerilogOptionTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(ServiceBusEntityType.Queue)]
        [InlineData(ServiceBusEntityType.Topic)]
        public async Task GetHealthOfServiceBusProject_WithExcludeSerilog_ResponseHealthy(ServiceBusEntityType resourceEntity)
        {
            // Arrange
            var config = TestConfig.Create();
            var options = 
                ServiceBusWorkerProjectOptions
                    .Create(config).WithExcludeSerilog();

            using (var project = await ServiceBusWorkerProject.StartNewAsync(resourceEntity, config, options, _outputWriter))
            {
                // Act
                HealthStatus status = await project.Health.ProbeHealthAsync();
                
                // Assert
                Assert.Equal(HealthStatus.Healthy, status);
            }
        }
    }
}
