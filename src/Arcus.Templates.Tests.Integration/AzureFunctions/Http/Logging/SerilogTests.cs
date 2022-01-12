using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Logging
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SerilogTests
    {
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogTests"/> class.
        /// </summary>
        public SerilogTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _configuration = TestConfig.Create();
        }

        [Fact]
        public async Task GetHealth_WithApplicationInsightsLogging_ReturnsOk()
        {
            // Arrange
            var options =
                new AzureFunctionsHttpProjectOptions()
                    .WithIncludeHealthChecks();

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(_configuration, options, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
                {
                    // Assert
                    Assert.NotNull(response);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetHealth_WithValidSerilogDefaultLogLevel_ReturnsOk()
        {
            // Arrange
            var options =
                new AzureFunctionsHttpProjectOptions()
                    .WithIncludeHealthChecks();

            Environment.SetEnvironmentVariable("AzureFunctionsJobHost__Serilog__MinimumLevel__Default", "Information");

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(_configuration, options, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
                {
                    // Assert
                    Assert.NotNull(response);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task StartProject_WithInvalidSerilogDefaultLogLevel_ThrowsException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("AzureFunctionsJobHost__Serilog__MinimumLevel__Default", "InvalidLogLevel");

            // Assert
            await Assert.ThrowsAsync<CannotStartTemplateProjectException>(
                // Act
                () => AzureFunctionsHttpProject.StartNewAsync(_configuration, _outputWriter)
            );
        }
    }
}
