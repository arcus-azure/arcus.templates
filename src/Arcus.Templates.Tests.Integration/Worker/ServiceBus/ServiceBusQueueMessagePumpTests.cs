using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Bogus;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
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
            var connectionString = _configuration.GetValue<string>("Arcus:Worker:ServiceBus:ConnectionString");
            var topicName = _configuration.GetValue<string>("Arcus:Worker:ServiceBus:TopicName");

            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);
            _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, new XunitTestLogger(_outputWriter));
        }

        [Fact]
        public async Task MinimServiceBusQueueWorker_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            using (var project = await ServiceBusQueueWorkerProject.StartNewAsync(_configuration, _outputWriter))
            {
                var connectionString = _configuration.GetValue<string>("Arcus:Worker:ServiceBus:ConnectionStringWithQueue");

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
