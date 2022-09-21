using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class EventHubsMessagePumpTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsMessagePumpTests" /> class.
        /// </summary>
        public EventHubsMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task EventHubsWorker_PublishEventData_MessageSuccessfullyProcessed()
        {
            await using (var project = await EventHubsWorkerProject.StartNewAsync(_outputWriter))
            {
                await project.MessagePump.SimulateMessageProcessingAsync();
            }
        }
    }
}
