using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Azure.ServiceBus;
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

        [Theory]
        [InlineData(ServiceBusEntityType.Topic, FunctionsWorker.Isolated)]
        [InlineData(ServiceBusEntityType.Topic, FunctionsWorker.InProcess)]
        [InlineData(ServiceBusEntityType.Queue, FunctionsWorker.InProcess)]
        public async Task ServiceBusProject_WithOrderMessageHandlerImplementation_CorrectlyProcessesMessage(ServiceBusEntityType entityType, FunctionsWorker workerType)
        {
            var config = TestConfig.Create();
            var options =
                new AzureFunctionsServiceBusProjectOptions(entityType)
                    .WithFunctionWorker(workerType);

            await using (var project = await AzureFunctionsServiceBusProject.StartNewProjectAsync(entityType, options, config, _outputWriter))
            {
                // Act / Assert
                await project.MessagePump.SimulateMessageProcessingAsync();
            }
        }
    }
}
