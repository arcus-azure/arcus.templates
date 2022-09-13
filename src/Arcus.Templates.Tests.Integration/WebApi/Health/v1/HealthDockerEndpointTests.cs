using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Health.v1
{
    [Collection(TestCollections.Docker)]
    [Trait("Category", TestTraits.Docker)]
    public class HealthDockerEndpointTests
    {
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthDockerEndpointTests"/> class.
        /// </summary>
        public HealthDockerEndpointTests(ITestOutputHelper outputWriter)
        {
            _configuration = TestConfig.Create();
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task Health_Get_Docker_Succeeds()
        {
            // Arrange
            Uri dockerBaseUrl = _configuration.GetDockerBaseUrl();
            var healthEndpointService = new HealthEndpointService(dockerBaseUrl, _outputWriter);

            // Act
            HealthStatus status = await healthEndpointService.GetHealthAsync();

            // Assert
            Assert.Equal(HealthStatus.Healthy, status);
        }
    }
}
