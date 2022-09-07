using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Logging;
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

        [Theory]
        [InlineData(ServiceBusEntityType.Queue)]
        [InlineData(ServiceBusEntityType.Topic)]
        public async Task ServiceBusProject_WithoutSerilog_CorrectlyProcessesMessage(ServiceBusEntityType entityType)
        {
            // Arrange
            var config = TestConfig.Create();
            var options =
                new AzureFunctionsServiceBusProjectOptions()
                    .WithExcludeSerilog();

            await using (var project = await AzureFunctionsServiceBusProject.StartNewProjectAsync(entityType, options, config, _outputWriter))
            {
                // Act / Assert
                await project.MessagePump.SimulateMessageProcessingAsync();

                string projectFileContents = project.GetFileContentsOfProjectFile();
                Assert.DoesNotContain(projectFileContents, "Serilog");
            }
        }
    }
}
