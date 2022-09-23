using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.EventHubs;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
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

        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task EventHubsProject_WithDefaultOptions_CorrectlyProcessesMessage(FunctionsWorker workerType)
        {
            // Arrange
            var config = TestConfig.Create();
            var options = new AzureFunctionsEventHubsProjectOptions().WithFunctionWorker(workerType);
            await using (var project = await AzureFunctionsEventHubsProject.StartNewAsync(config, options, _outputWriter))
            {
                // Act / Assert
                await project.MessagePump.SimulateMessageProcessingAsync();
            }
        }

        [Fact]
        public async Task SendEventToEventHubs()
        {
            var configuration = TestConfig.Create();
            EventHubsConfig eventHubsConfig = configuration.GetEventHubsConfig();
            var producer = new TestEventHubsMessageProducer(eventHubsConfig.EventHubsName, eventHubsConfig.EventHubsConnectionString);
            await using var service = new MessagePumpService(producer, configuration, _outputWriter);
            await service.StartAsync();
            await service.SimulateMessageProcessingAsync();
        }
    }
}
