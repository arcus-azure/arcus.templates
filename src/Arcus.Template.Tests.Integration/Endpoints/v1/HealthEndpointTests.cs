using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Template.Tests.Integration.Fixture;
using Xunit;

namespace Arcus.Template.Tests.Integration.Endpoints.v1
{
    [Collection("Integration")]
    public class HealthEndpointTests
    {
        private readonly TestConfig _configuration;
        private readonly HealthEndpointService _healthService;

        public HealthEndpointTests()
        {
            _configuration = TestConfig.Create();
            _healthService = new HealthEndpointService(_configuration);
        }

        [Fact]
        public async Task Health_Get_Succeeds()
        {
            // Act
            using (HttpResponseMessage response = await _healthService.GetAsync())
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
