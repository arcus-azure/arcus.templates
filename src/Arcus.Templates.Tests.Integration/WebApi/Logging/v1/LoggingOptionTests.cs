using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Logging.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class LoggingOptionTests
    {
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingOptionTests"/> class.
        /// </summary>
        public LoggingOptionTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _configuration = TestConfig.Create();   
        }

        [Fact]
        public async Task GetHealth_WithConsoleLoggingProjectOption_ReturnsOk()
        {
            // Arrange
            var optionsWithDefaultLogging =
                new WebApiProjectOptions().WithConsoleLogging();

            using (var project = await WebApiProject.StartNewAsync(optionsWithDefaultLogging, _outputWriter)) 
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetHealth_WithSerilogLoggingProjectOption_ReturnsOk()
        {
            // Arrange
            var optionsWithSerilogLogging =
                new WebApiProjectOptions().WithSerilogLogging();

            using (var project = await WebApiProject.StartNewAsync(optionsWithSerilogLogging, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetHealth_WithSerilogLoggingWithoutCorrelationProjectOption_ReturnsOk()
        {
            // Arrange
            var optionsWithSerilogLogging =
                new WebApiProjectOptions()
                    .WithSerilogLogging()
                    .WithExcludeCorrelation();

            using (var project = await WebApiProject.StartNewAsync(optionsWithSerilogLogging, _outputWriter))
                // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
