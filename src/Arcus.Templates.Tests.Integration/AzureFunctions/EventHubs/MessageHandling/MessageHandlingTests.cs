using System.Threading.Tasks;
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
        public async Task EventHubsProject_WithDefault_CorrectlyProcessesMessage()
        {
            // Arrange
            await using (var project = await AzureFunctionsEventHubsProject.StartNewAsync(_outputWriter))
            {
                // Act / Assert
                await project.Messaging.SimulateMessageProcessingAsync();
            }
        }
    }
}
