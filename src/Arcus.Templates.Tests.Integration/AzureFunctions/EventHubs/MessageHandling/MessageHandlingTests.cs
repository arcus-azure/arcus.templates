using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.EventHubs.MessageHandling
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class MessageHandlingTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlingTests" /> class.
        /// </summary>
        public MessageHandlingTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task EventHubsProject_AsIsolated_CorrectlyProcessesMessage()
        {
            await TestEventHubsProjectWithWorkerTypeCorrectlyProcessesMessage(FunctionsWorker.Isolated);
        }

        [Fact]
        public async Task EventHubsProject_AsInProcess_CorrectlyProcessesMessage()
        {
            await TestEventHubsProjectWithWorkerTypeCorrectlyProcessesMessage(FunctionsWorker.InProcess);
        }

        private async Task TestEventHubsProjectWithWorkerTypeCorrectlyProcessesMessage(FunctionsWorker workerType)
        {
            // Arrange
            var config = TestConfig.Create();
            var options = new AzureFunctionsEventHubsProjectOptions().WithFunctionWorker(workerType);
            await using (var project = await AzureFunctionsEventHubsProject.StartNewAsync(config, options, _outputWriter))
            {
                // Act / Assert
                await project.Messaging.SimulateMessageProcessingAsync();
            }
        }
    }
}
