using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.WebApi.Health.v1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Health
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class HealthChecksTests
    {
        private readonly TestConfig _config;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthChecksTests" /> class.
        /// </summary>
        public HealthChecksTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task HttpAzureFunctionsProject_WithIncludeHealthChecks_ContainsHealthChecks()
        {
            // Arrange
            var options =
                new AzureFunctionsHttpProjectOptions()
                    .WithIncludeHealthChecks();
            
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(_config, options, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.NotEmpty(response.Headers.GetValues("RequestId"));
                    Assert.NotEmpty(response.Headers.GetValues("X-Transaction-Id"));
                    
                    string healthReportJson = await response.Content.ReadAsStringAsync();

                    var healthReport = JsonConvert.DeserializeObject<HealthReport>(healthReportJson, new TimeSpanConverter());
                    Assert.NotNull(healthReport);
                    Assert.Equal(HealthStatus.Healthy, healthReport.Status);
                }
            }
        }
        
        [Fact]
        public async Task HttpAzureFunctionsProject_WithIncludeHealthChecks_ChecksAcceptRequestHeader()
        {
            // Arrange
            var options =
                new AzureFunctionsHttpProjectOptions()
                    .WithIncludeHealthChecks();
            
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(_config, options, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync(request =>
                {
                    request.Headers.Accept.Clear();
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
                }))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task HttpAzureFunctionsProject_WithoutOptions_DoesNotContainHealthChecks()
        {
            // Arrange
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(_config, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                }
            }
        }
    }
}
