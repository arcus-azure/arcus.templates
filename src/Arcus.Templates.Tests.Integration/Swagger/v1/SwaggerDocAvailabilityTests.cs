using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Swagger.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SwaggerDocAvailabilityTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerDocAvailabilityTests"/> class.
        /// </summary>
        public SwaggerDocAvailabilityTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(BuildConfiguration.Debug, HttpStatusCode.OK)]
        [InlineData(BuildConfiguration.Release, HttpStatusCode.NotFound)]
        public async Task GetSwaggerUI_WithBuildConfiguration_Returns(BuildConfiguration buildConfiguration, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var configuration = TestConfig.Create(buildConfiguration);
            using (var project = await WebApiProject.StartNewAsync(configuration, _outputWriter)) 
            // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerUI())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(expectedStatusCode, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(BuildConfiguration.Debug, HttpStatusCode.OK)]
        [InlineData(BuildConfiguration.Release, HttpStatusCode.NotFound)]
        public async Task GetSwaggerDocs_WithBuildConfiguration_Returns(BuildConfiguration buildConfiguration, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var configuration = TestConfig.Create(buildConfiguration);
            using (var project = await WebApiProject.StartNewAsync(configuration, _outputWriter))
            // Act
            using (var response = await project.Swagger.GetSwaggerDocs())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(expectedStatusCode, response.StatusCode);
            }
        }
    }
}
