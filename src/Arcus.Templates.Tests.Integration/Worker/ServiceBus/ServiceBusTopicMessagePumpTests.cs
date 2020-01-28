using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class ServiceBusTopicMessagePumpTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusQueueMessagePumpTests"/> class.
        /// </summary>
        public ServiceBusTopicMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task MinimServiceBusQueueWorker_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            await using (var project = await ServiceBusWorkerProject.StartNewWithTopicAsync(_outputWriter))
            {
                // Act / Assert
                await project.MessagePump.SimulateMessageProcessingAsync();
            }
        }
    }
}