using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus;
using Azure.Messaging.ServiceBus;
using Bogus;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.MessagePump
{
    /// <summary>
    /// Represents a service to interact with the hosted-service.
    /// </summary>
    public class MessagePumpService : IAsyncDisposable
    {
        private readonly ServiceBusEntityType _entityType;
        private readonly ILogger _logger;
        private readonly TestConfig _configuration;

        private TestServiceBusMessageEventConsumer _serviceBusMessageEventConsumer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePumpService"/> class.
        /// </summary>
        public MessagePumpService(ServiceBusEntityType entityType, TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            _entityType = entityType;
            _logger = new XunitTestLogger(outputWriter);
            _configuration = configuration;
        }

        /// <summary>
        /// Starts a new instance of the <see cref="MessagePumpService"/> type to simulate messages.
        /// </summary>
        public async Task StartAsync()
        {
            if (_serviceBusMessageEventConsumer is null)
            {
                _serviceBusMessageEventConsumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_configuration, _logger);
            }
            else
            {
                throw new InvalidOperationException("Service is already started!");
            }
        }

        /// <summary>
        /// Simulate the message processing of the message pump using the Service Bus.
        /// </summary>
        public async Task SimulateMessageProcessingAsync()
        {
            if (_serviceBusMessageEventConsumer is null)
            {
                throw new InvalidOperationException(
                    "Cannot simulate the message pump because the service is not yet started; please start this service before simulating");
            }

            string connectionString = _configuration.GetServiceBusConnectionString(_entityType);
            var producer = new TestServiceBusMessageProducer(connectionString);

            var operationId = $"operation-{Guid.NewGuid()}";
            var transactionId = $"transaction-{Guid.NewGuid()}";
            Order order = GenerateOrder();
            ServiceBusMessage orderMessage = order.AsServiceBusMessage(operationId, transactionId);
            await producer.ProduceAsync(orderMessage);

            OrderCreatedEventData orderCreatedEventData = _serviceBusMessageEventConsumer.ConsumeOrderEvent(operationId);
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
             var customerGenerator = new Faker<Customer>()
                .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName())
                .RuleFor(u => u.LastName, (f, u) => f.Name.LastName());

            var orderGenerator = new Faker<Order>()
                .RuleFor(u => u.Customer, () => customerGenerator)
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
            if (_serviceBusMessageEventConsumer != null)
            {
                await _serviceBusMessageEventConsumer.DisposeAsync();
            }
        }
    }
}
