using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.EventHubs.Logging
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SerilogLoggingTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogLoggingTests" /> class.
        /// </summary>
        public SerilogLoggingTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task EventHubsProject_WithoutSerilog_CorrectlyProcessesMessage()
        {
            // Arrange
            var config = TestConfig.Create();
            var options = new AzureFunctionsEventHubsProjectOptions()
                .ExcludeSerilog();

            // Act
            using (var project = await AzureFunctionsEventHubsProject.StartNewAsync(config, options, _outputWriter))
            {
                // Assert
                await project.Messaging.SimulateMessageProcessingAsync();
                Assert.DoesNotContain("Serilog", project.GetFileContentsOfProjectFile());
            }
        }
    }
}
