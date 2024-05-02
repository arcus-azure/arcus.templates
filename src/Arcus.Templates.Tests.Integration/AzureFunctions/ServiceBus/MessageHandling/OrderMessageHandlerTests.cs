using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus.MessageHandling
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class OrderMessageHandlerTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderMessageHandlerTests" /> class.
        /// </summary>
        public OrderMessageHandlerTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task ServiceBusTopicProject_AsIsolated_CorrectlyProcessesMessage()
        {
           await TestServiceBusProjectWithWorkerTypeCorrectlyProcessesMessageAsync(ServiceBusEntityType.Topic, FunctionsWorker.Isolated);
        }

        [Fact]
        public async Task ServiceBusQueueProject_AsInProcess_CorrectlyProcessesMessage()
        {
            await TestServiceBusProjectWithWorkerTypeCorrectlyProcessesMessageAsync(ServiceBusEntityType.Queue, FunctionsWorker.InProcess);
        }

        [Fact]
        public async Task ServiceBusQueueProject_AsIsolated_CorrectlyProcessesMessage()
        {
            await TestServiceBusProjectWithWorkerTypeCorrectlyProcessesMessageAsync(ServiceBusEntityType.Queue, FunctionsWorker.Isolated);
        }

        private async Task TestServiceBusProjectWithWorkerTypeCorrectlyProcessesMessageAsync(ServiceBusEntityType entityType, FunctionsWorker workerType)
        {
            // Arrange
            var config = TestConfig.Create();
            var options =
                new AzureFunctionsServiceBusProjectOptions(entityType)
                    .WithFunctionWorker(workerType);

            await using (var project = await AzureFunctionsServiceBusProject.StartNewProjectAsync(entityType, options, config, _outputWriter))
            {
                // Act / Assert
                await project.Messaging.SimulateMessageProcessingAsync();
            }
        }
    }
}
