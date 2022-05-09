using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus.Logging
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
        public async Task ServiceBusQueueProject_WithoutSerilog_CorrectlyProcessesMessage()
        {
            // Arrange
            var config = TestConfig.Create();
            var options =
                new AzureFunctionsServiceBusProjectOptions()
                    .WithExcludeSerilog();

            await using (var project = await AzureFunctionsServiceBusProject.StartNewQueueProjectAsync(options, config, _outputWriter))
            {
                // Act / Assert
                await project.MessagePump.SimulateMessageProcessingAsync();

                string projectFileContents = project.GetFileContentsOfProjectFile();
                Assert.DoesNotContain(projectFileContents, "Serilog");
            }
        }
    }
}
