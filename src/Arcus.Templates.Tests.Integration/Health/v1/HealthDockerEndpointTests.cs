using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Health.v1
{
    [Collection("Docker")]
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
            using (HttpResponseMessage response = await healthEndpointService.GetAsync())
            {
                // Assert
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                
                HealthReport healthReport = await response.Content.ReadAsAsync<HealthReport>();
                Assert.NotNull(healthReport);
                Assert.Equal(HealthStatus.Healthy, healthReport.Status);
            }
        }
    }
}
