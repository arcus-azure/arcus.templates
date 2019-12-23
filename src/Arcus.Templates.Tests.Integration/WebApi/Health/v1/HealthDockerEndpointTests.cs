using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
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
            using (HttpResponseMessage response = await healthEndpointService.GetAsync())
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string healthReportJson = await response.Content.ReadAsStringAsync();
                var healthReport = JsonConvert.DeserializeObject<HealthReport>(healthReportJson);
                Assert.NotNull(healthReport);
                Assert.Equal(HealthStatus.Healthy, healthReport.Status);
            }
        }
    }
}
