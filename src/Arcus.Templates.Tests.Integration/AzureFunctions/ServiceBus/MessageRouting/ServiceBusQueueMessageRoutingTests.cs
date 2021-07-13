using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus.MessageRouting
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class ServiceBusQueueMessageRoutingTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusQueueMessageRoutingTests"/> class.
        /// </summary>
        public ServiceBusQueueMessageRoutingTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task AzureFunctionsServiceBusQueueProject_WithoutOptions_ProcessesMessageSuccessfully()
        {
            // Arrange
            await using (var project = await AzureFunctionsServiceBusProject.StartNewWithQueueAsync( _outputWriter))
            {
                // Act / Assert
                await project.MessagePump.SimulateMessageProcessingAsync();
            }
        }
    }
}
