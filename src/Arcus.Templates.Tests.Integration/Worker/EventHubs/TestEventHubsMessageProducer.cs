using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs
{
    /// <summary>
    /// Represents a message consumer to send messages to Azure EventHubs.
    /// </summary>
    public class TestEventHubsMessageProducer : IOrderProducer
    {
        private readonly string _eventHubsName;
        private readonly string _eventHubsConnectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestEventHubsMessageProducer" /> class.
        /// </summary>
        /// <param name="eventHubsName">The name of the Azure EventHubs resource.</param>
        /// <param name="eventHubsConnectionString">The connection string to interact with the Azure EventHubs resource.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventHubsName"/> or the <paramref name="eventHubsConnectionString"/> is blank.</exception>
        public TestEventHubsMessageProducer(string eventHubsName, string eventHubsConnectionString)
        {
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank name of the Azure EventHubs resource");
            Guard.NotNullOrWhitespace(eventHubsConnectionString, nameof(eventHubsConnectionString), "Requires a non-blank connection string to interact with the Azure EventHubs resource");
            
            _eventHubsName = eventHubsName;
            _eventHubsConnectionString = eventHubsConnectionString;
        }

        /// <summary>
        /// Sends the <paramref name="order"/> to the configured Azure EventHubs.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="operationId">The ID to identify a single operation in a correlation scenario.</param>
        /// <param name="transactionId">The ID to identify a whole transaction across interactions in a correlation scenario.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="order"/> is <c>null</c>.</exception>
        public async Task ProduceAsync(Order order, string operationId, string transactionId)
        {
            Guard.NotNull(order, nameof(order), "Requires an Azure EventHubs message to send");

            EventData message = 
                EventDataBuilder.CreateForBody(order)
                                .WithOperationId(operationId)
                                .WithTransactionId(transactionId).Build();

            await using (var client = new EventHubProducerClient(_eventHubsConnectionString, _eventHubsName))
            {
                await client.SendAsync(new[] { message });
            }
        }
    }
}
