using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Bogus;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus
{
    public class Order
    {
        [JsonProperty]
        public string Id { get; set; }

        [JsonProperty]
        public int Amount { get; set; }

        [JsonProperty]
        public string ArticleNumber { get; set; }
    }

    public class OrderCreatedEventData
    {
        public OrderCreatedEventData(string id, int amount, string articleNumber, MessageCorrelationInfo correlationInfo)
        {
            Id = id;
            Amount = amount;
            ArticleNumber = articleNumber;
            CorrelationInfo = correlationInfo;
        }

        public string Id { get; set; }
        public int Amount { get; set; }
        public string ArticleNumber { get; set; }
        public MessageCorrelationInfo CorrelationInfo { get; set; }
    }

    public class OrderCreatedEvent : EventGridEvent<OrderCreatedEventData>
    {
        private const string DefaultDataVersion = "1";
        private const string DefaultEventType = "Arcus.Samples.Orders.OrderCreated";

        public OrderCreatedEvent(string eventId, string orderId, int amount, string articleNumber, MessageCorrelationInfo correlationInfo)
            : base(eventId, subject: "order-created",
                   new OrderCreatedEventData(orderId, amount, articleNumber, correlationInfo),
                   DefaultDataVersion,
                   DefaultEventType)
        {
        }

        [JsonConstructor]
        private OrderCreatedEvent()
        {
        }
    }

    [Collection(TestCollections.Docker)]
    [Trait("Category", TestTraits.Docker)]
    public class ServiceBusQueueMessagePumpTests : IAsyncLifetime
    {
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusQueueMessagePumpTests"/> class.
        /// </summary>
        public ServiceBusQueueMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _configuration = TestConfig.Create();
            _outputWriter = outputWriter;
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            var connectionString = _configuration.GetValue<string>("Arcus:Worker:ServiceBus:ConnectionStringWithQueue");
            var topicName = _configuration.GetValue<string>("Arcus:Worker:ServiceBus:TopicName");

            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);
            _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, new XunitTestLogger(_outputWriter));
        }

        [Fact]
        public async Task MinimServiceBusQueueWorkerOnDocker_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var configuration = TestConfig.Create();
            string connectionString = configuration.GetValue<string>("Arcus:Worker:ServiceBus:ConnectionStringWithQueue");

            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var messageSender = new MessageSender(new ServiceBusConnectionStringBuilder(connectionString));

            Order order = GenerateOrder();
            Message orderMessage = order.WrapInServiceBusMessage(operationId, transactionId);

            // Act
            await messageSender.SendAsync(orderMessage);

            // Assert
            string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId);
            Assert.NotEmpty(receivedEvent);
            
            EventGridEventBatch<OrderCreatedEvent> deserializedEventGridMessage = EventGridParser.Parse<OrderCreatedEvent>(receivedEvent);
            Assert.NotNull(deserializedEventGridMessage);
            OrderCreatedEvent orderCreatedEvent = Assert.Single(deserializedEventGridMessage.Events);
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
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            await _serviceBusEventConsumerHost.StopAsync();
        }
    }
}
