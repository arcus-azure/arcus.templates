using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture
{
    public class TestServiceBusMessagePumpService : IMessagingService
    {
        private readonly ServiceBusEntityType _entityType;
        private readonly TestConfig _configuration;
        private readonly ILogger _logger;

        private TestServiceBusMessageEventConsumer _serviceBusMessageEventConsumer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceBusMessagePumpService" /> class.
        /// </summary>
        public TestServiceBusMessagePumpService(
            ServiceBusEntityType entityType,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
        {
            _entityType = entityType;
            _configuration = configuration;
            _logger = new XunitTestLogger(outputWriter);
        }

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

        public async Task SimulateMessageProcessingAsync()
        {
            if (_serviceBusMessageEventConsumer is null)
            {
                throw new InvalidOperationException(
                    "Cannot simulate the message pump because the service is not yet started; please start this service before simulating");
            }

            var operationId = $"operation-{Guid.NewGuid()}";
            var transactionId = $"transaction-{Guid.NewGuid()}";
            Order order = GenerateOrder();
            await ProduceMessageAsync(order, operationId, transactionId);

            var orderCreatedEventData = _serviceBusMessageEventConsumer.ConsumeEvent<OrderCreatedEventData>(operationId);
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

        private async Task ProduceMessageAsync(Order order, string operationId, string transactionId)
        {
            ServiceBusMessage message =
                ServiceBusMessageBuilder.CreateForBody(order)
                                        .WithOperationId(operationId)
                                        .WithTransactionId(transactionId)
                                        .Build();

            string connectionString = _configuration.GetServiceBusConnectionString(_entityType);
            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(connectionString);
            await using (var client = new ServiceBusClient(connectionString))
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

        public async ValueTask DisposeAsync()
        {
            if (_serviceBusMessageEventConsumer != null)
            {
                await _serviceBusMessageEventConsumer.DisposeAsync();
            }
        }
    }
}
