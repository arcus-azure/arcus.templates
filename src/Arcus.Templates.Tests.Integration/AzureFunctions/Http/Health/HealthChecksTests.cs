using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.WebApi.Health.v1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                    Assert.NotEmpty(response.Headers.GetValues("X-Transaction-Id"));
                    
                    string healthReportJson = await response.Content.ReadAsStringAsync();

                    var healthReport = JObject.Parse(healthReportJson);
                    Assert.Equal(HealthStatus.Healthy.ToString(), healthReport["status"]);
                }
            }
        }

        [Fact]
        public async Task HttpAzureFunctionsProject_RemovesExceptionDetails_WhenRequestingHealth()
        {
            // Arrange
            var options =
                new AzureFunctionsHttpProjectOptions()
                    .WithIncludeHealthChecks();

            using (var project = AzureFunctionsHttpProject.CreateNew(_config, options, _outputWriter))
            {
                string description = "Sabotage this!";
                project.UpdateFileWithUsingStatement("Startup.cs", typeof(HealthCheckResult));
                project.UpdateFileInProject("Startup.cs", contents =>
                {
                    return contents.Replace(
                        "builder.Services.AddHealthChecks();",
                        "builder.Services.AddHealthChecks()" 
                        + $".AddCheck(\"sample\", () => HealthCheckResult.Unhealthy(\"{description}\", new InvalidOperationException(\"Sabotage!\")));");
                });
                await project.StartAsync();
                project.TearDownOptions = TearDownOptions.KeepProjectDirectory;
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

                    string json = await response.Content.ReadAsStringAsync();
                    var report = JObject.Parse(json);
                    Assert.Equal(HealthStatus.Unhealthy.ToString(), report["status"]);
                    Assert.True(report.TryGetValue("entries", out JToken entries), "Health report should contain 'entries' property");
                    JToken sampleEntry = entries["sample"];
                    Assert.Equal(description, sampleEntry["description"]);
                    Assert.Null(sampleEntry["exception"]);
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
