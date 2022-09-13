using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json.Linq;
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
        public async Task GetHealth_WithoutOptions_Succeeds()
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

        [Fact]
        public async Task GetHealth_WithCustomApiHealthReport_RemovesExceptions()
        {
            // Arrange
            using (var project = WebApiProject.CreateNew(_outputWriter))
            {
                string descripton = "Sabotage this!";
                project.UpdateFileInProject("Program.cs", contents =>
                {
                    return contents.Replace(
                        "builder.Services.AddHealthChecks();",
                        "builder.Services.AddHealthChecks()" 
                         + $".AddCheck(\"sample\", () => HealthCheckResult.Unhealthy(\"{descripton}\", new InvalidOperationException(\"Sabotage!\")));");
                });
                await project.StartAsync();

                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                    
                    string contents = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(contents);
                    Assert.True(json.TryGetValue("entries", out JToken entries), "Health report should contain 'entries' property");
                    JToken sampleEntry = entries["sample"];
                    Assert.NotNull(sampleEntry);
                    Assert.Equal(descripton, sampleEntry["description"]);
                    Assert.Null(sampleEntry["exception"]);
                }
            }
        }
    }
}