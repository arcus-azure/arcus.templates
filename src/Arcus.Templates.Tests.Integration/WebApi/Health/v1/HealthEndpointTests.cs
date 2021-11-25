using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
                HealthStatus status = await project.Health.GetHealthAsync();

                // Assert
                Assert.Equal(HealthStatus.Healthy, status);
            }
        }
    }
}