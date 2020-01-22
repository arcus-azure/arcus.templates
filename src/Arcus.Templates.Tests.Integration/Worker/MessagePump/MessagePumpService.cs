using System;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Bogus;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.MessagePump
{
    /// <summary>
    /// Represents a service to interact with the hosted-service.
    /// </summary>
    public class MessagePumpService : IAsyncDisposable
    {
        private readonly ITestOutputHelper _outputWriter;
        private readonly TestConfig _configuration;

        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePumpService"/> class.
        /// </summary>
        public MessagePumpService(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            _outputWriter = outputWriter;
            _configuration = configuration;
        }

        /// <summary>
        /// Starts a new instance of the <see cref="MessagePumpService"/> type to simulate messages.
        /// </summary>
        public async Task StartAsync()
        {
            if (_serviceBusEventConsumerHost is null)
            {
                var topicName = _configuration.GetValue<string>("Arcus:Worker:Infra:ServiceBus:TopicName");
                var connectionString = _configuration.GetValue<string>("Arcus:Worker:Infra:ServiceBus:ConnectionString");
                var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);

                _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, new XunitTestLogger(_outputWriter));
            }
            else
            {
                throw new InvalidOperationException("Service is already started!");
            }
        }

        /// <summary>
        /// Simulate the message processing of the message pump using the Service Bus Queue.
        /// </summary>
        public async Task SimulateMessageProcessingWithQueueAsync()
        {
            await SimulateMessageProcessingAsync("Arcus:Worker:ServiceBus:ConnectionStringWithQueue");
        }

        /// <summary>
        /// Simulate the message processing of the message pump using the Service Bus Topic.
        /// </summary>
        public async Task SimulateMessageProcessingWithTopicAsync()
        {
            await SimulateMessageProcessingAsync("Arcus:Worker:ServiceBus:ConnectionStringWithTopic");
        }

        private async Task SimulateMessageProcessingAsync(string connectionStringConfigurationKey)
        {
            if (_serviceBusEventConsumerHost is null)
            {
                throw new InvalidOperationException(
                    "Cannot simulate the message pump because the service is not yet started; please start this service before simulating");
            }

            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();

            var connectionString = _configuration.GetValue<string>(connectionStringConfigurationKey);
            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            var messageSender = new MessageSender(serviceBusConnectionStringBuilder);

            Order order = GenerateOrder();
            Message orderMessage = order.WrapInServiceBusMessage(operationId, transactionId);
            await messageSender.SendAsync(orderMessage);

            string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId);
            Assert.NotEmpty(receivedEvent);

            EventGridEventBatch<OrderCreatedEvent> eventBatch = EventGridParser.Parse<OrderCreatedEvent>(receivedEvent);
            Assert.NotNull(eventBatch);
            OrderCreatedEvent orderCreatedEvent = Assert.Single(eventBatch.Events);
            Assert.NotNull(orderCreatedEvent);

            var orderCreatedEventData = orderCreatedEvent.GetPayload<OrderCreatedEventData>();
            Assert.NotNull(orderCreatedEventData);
            Assert.NotNull(orderCreatedEventData.CorrelationInfo);
            Assert.Equal(order.Id, orderCreatedEventData.Id);
            Assert.Equal(order.Amount, orderCreatedEventData.Amount);
            Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
            Assert.Equal(transactionId, orderCreatedEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, orderCreatedEventData.CorrelationInfo.OperationId);
            Assert.NotEmpty(orderCreatedEventData.CorrelationInfo.CycleId);
        }

        private static Order GenerateOrder()
        {
            var orderGenerator = new Faker<Order>()
                .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
                .RuleFor(u => u.Amount, f => f.Random.Int())
                .RuleFor(u => u.ArticleNumber, f => f.Commerce.Product());

            return orderGenerator.Generate();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_serviceBusEventConsumerHost != null)
            {
                await _serviceBusEventConsumerHost.StopAsync();
            }
        }
    }
}
