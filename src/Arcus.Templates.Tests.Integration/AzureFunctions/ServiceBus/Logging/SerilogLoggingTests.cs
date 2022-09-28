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

        [Fact]
        public async Task ServiceBusTopicProjectIsolated_WithoutSerilog_CorrectlyProcessesMessage()
        {
            await TestServiceBusProjectWithoutSerilogCorrectlyProcessesMessage(ServiceBusEntityType.Topic, FunctionsWorker.Isolated);
        }

        [Fact]
        public async Task ServiceBusTopicProjectInProcess_WithoutSerilog_CorrectlyProcessesMessage()
        {
            await TestServiceBusProjectWithoutSerilogCorrectlyProcessesMessage(ServiceBusEntityType.Topic, FunctionsWorker.InProcess);
        }

        [Fact]
        public async Task ServiceBusQueueProjectInProcess_WithoutSerilog_CorrectlyProcessesMessage()
        {
            await TestServiceBusProjectWithoutSerilogCorrectlyProcessesMessage(ServiceBusEntityType.Queue, FunctionsWorker.InProcess);
        }

        private async Task TestServiceBusProjectWithoutSerilogCorrectlyProcessesMessage(ServiceBusEntityType entityType, FunctionsWorker workerType)
        {
            // Arrange
            var config = TestConfig.Create();
            var options =
                new AzureFunctionsServiceBusProjectOptions(entityType)
                    .WithFunctionWorker(workerType)
                    .WithExcludeSerilog();

            await using (var project = await AzureFunctionsServiceBusProject.StartNewProjectAsync(entityType, options, config, _outputWriter))
            {
                // Act / Assert
                await project.MessagePump.SimulateMessageProcessingAsync();
                Assert.DoesNotContain("Serilog", project.GetFileContentsOfProjectFile());
                Assert.DoesNotContain("Serilog", project.GetFileContentsInProject("Program.cs"));
            }
        }
    }
}
