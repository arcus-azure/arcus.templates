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
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class HealthEndpointTests
    {
        private readonly ITestOutputHelper _outputWriter;

        public HealthEndpointTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task Health_Get_Succeeds()
        {
            // Arrange
            using (WebApiProject project = await WebApiProject.StartNewAsync(_outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
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
}