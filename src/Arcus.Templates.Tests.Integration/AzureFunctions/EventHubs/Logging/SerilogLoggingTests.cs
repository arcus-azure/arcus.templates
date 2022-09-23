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

        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task EventHubsProject_WithoutSerilog_CorrectlyProcessesMessage(FunctionsWorker workerType)
        {
            // Arrange
            var config = TestConfig.Create();
            var options = new AzureFunctionsEventHubsProjectOptions()
                .WithFunctionWorker(workerType)
                .ExcludeSerilog();

            // Act
            await using (var project = await AzureFunctionsEventHubsProject.StartNewAsync(config, options, _outputWriter))
            {
                project.TearDownOptions = TearDownOptions.KeepProjectDirectory;
                // Assert
                await project.MessagePump.SimulateMessageProcessingAsync();
                Assert.DoesNotContain("Serilog", project.GetFileContentsOfProjectFile());
            }
        }
    }
}
