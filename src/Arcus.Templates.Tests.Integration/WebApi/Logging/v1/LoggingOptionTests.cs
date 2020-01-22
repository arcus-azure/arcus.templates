using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Logging.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class LoggingOptionTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingOptionTests"/> class.
        /// </summary>
        public LoggingOptionTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(WebApiLogging.Default)]
        [InlineData(WebApiLogging.Serilog)]
        public async Task GetHealth_WithSerilogProjectOption_ReturnsOk(WebApiLogging logging)
        {
            // Arrange
            var optionsWithSerilog =
                new WebApiProjectOptions().WithLogging(logging);

            using (var project = await WebApiProject.StartNewAsync(optionsWithSerilog, _outputWriter))
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
