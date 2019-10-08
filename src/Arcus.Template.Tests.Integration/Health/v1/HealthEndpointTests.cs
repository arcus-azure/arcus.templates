using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Template.Tests.Integration.Fixture;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Template.Tests.Integration.Health.v1
{
    [Collection("Integration")]
    public class HealthEndpointTests
    {
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        public HealthEndpointTests(ITestOutputHelper outputWriter)
        {
            _configuration = TestConfig.Create();
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task Health_Get_Succeeds()
        {
            // Act
            using (var project = await WebApiProject.StartNewAsync(_configuration, _outputWriter))
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var healthReport = await response.Content.ReadAsAsync<HealthReport>();
                Assert.NotNull(healthReport);
                Assert.Equal(HealthStatus.Healthy, healthReport.Status);
            }
        }
    }
}
