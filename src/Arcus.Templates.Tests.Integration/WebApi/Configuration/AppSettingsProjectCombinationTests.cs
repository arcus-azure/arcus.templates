using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Configuration
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class AppSettingsProjectCombinationTests
    {
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsProjectCombinationTests"/> class.
        /// </summary>
        public AppSettingsProjectCombinationTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _configuration = TestConfig.Create();
        }

        [Fact]
        public async Task GetHealth_WithSerilogAndCertificateAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            string subject = $"subject-{Guid.NewGuid()}";
            string instrumentationKey = _configuration.GetApplicationInsightsInstrumentationKey();
            var optionsWithSerilogAndCertificateAuth =
                new WebApiProjectOptions()
                    .WithSerilogLogging(instrumentationKey)
                    .WithCertificateSubjectAuthentication($"CN={subject}");

            using (var project = await WebApiProject.StartNewAsync(_configuration, optionsWithSerilogAndCertificateAuth, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }
    }
}
