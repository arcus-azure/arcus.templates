using System;
using System.Threading.Tasks;
using Arcus.Templates.AzureFunctions.Http.Model;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.MessagePump;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Order = Arcus.Templates.Tests.Integration.Worker.Fixture.Order;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus
{
    /// <summary>
    /// Represents an event producer which sends events to an Azure Service Bus.
    /// </summary>
    public class TestServiceBusMessageProducer : IOrderProducer
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceBusMessageProducer"/> class.
        /// </summary>
        /// <param name="connectionString">The Azure Service Bus entity-scoped connection string to send messages to.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString"/> is blank.</exception>
        public TestServiceBusMessageProducer(string connectionString)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires a non-blank Azure Service Bus entity-scoped connection string");
            _connectionString = connectionString;
        }

        /// <summary>
        /// Sends the <paramref name="order"/> to the configured Azure Service Bus.
        /// </summary>
        /// <param name="order">The message to send.</param>
        /// <param name="operationId">The ID to identify a single operation in a correlation scenario.</param>
        /// <param name="transactionId">The ID to identify a whole transaction across interactions in a correlation scenario.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="order"/> is <c>null</c>.</exception>
        public async Task ProduceAsync(Order order, string operationId, string transactionId)
        {
            Guard.NotNull(order, nameof(order), "Requires an Azure Service Bus message to send");

            ServiceBusMessage message =
                ServiceBusMessageBuilder.CreateForBody(order)
                                        .WithOperationId(operationId)
                                        .WithTransactionId(transactionId)
                                        .Build();

            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(_connectionString);
            await using (var client = new ServiceBusClient(_connectionString))
            {
                ServiceBusSender messageSender = client.CreateSender(connectionStringProperties.EntityPath);

                try
                {
                    await messageSender.SendMessageAsync(message);
                }
                finally
                {
                    await messageSender.CloseAsync();
                }
            }
        }
    }
}
