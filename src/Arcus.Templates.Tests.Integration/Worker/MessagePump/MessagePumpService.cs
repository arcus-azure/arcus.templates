using System;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Azure.Messaging.ServiceBus;
using Bogus;
using GuardNet;
using Microsoft.Azure.ServiceBus;
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
        private readonly ServiceBusEntity _entity;
        private readonly ITestOutputHelper _outputWriter;
        private readonly TestConfig _configuration;

        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePumpService"/> class.
        /// </summary>
        public MessagePumpService(ServiceBusEntity entity, TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            _entity = entity;
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
        /// Simulate the message processing of the message pump using the Service Bus.
        /// </summary>
        public async Task SimulateMessageProcessingAsync()
        {
            if (_serviceBusEventConsumerHost is null)
            {
                throw new InvalidOperationException(
                    "Cannot simulate the message pump because the service is not yet started; please start this service before simulating");
            }

            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();

            string connectionString = _configuration.GetServiceBusConnectionString(_entity);
            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(connectionString);
            
            await using (var client = new ServiceBusClient(connectionString))
            await using (ServiceBusSender messageSender = client.CreateSender(connectionStringProperties.EntityPath))
            {
                try
                {
                    Order order = GenerateOrder();
                    ServiceBusMessage orderMessage = order.AsServiceBusMessage(operationId, transactionId);
                    await messageSender.SendMessageAsync(orderMessage);

                    string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId);
                    Assert.NotEmpty(receivedEvent);

                    EventBatch<Event> eventBatch = EventParser.Parse(receivedEvent);
                    Assert.NotNull(eventBatch);
                    Event orderCreatedEvent = Assert.Single(eventBatch.Events);
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
                finally
                {
                    await messageSender.CloseAsync();
                }
            }
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
            if (_serviceBusEventConsumerHost != null)
            {
                await _serviceBusEventConsumerHost.StopAsync();
            }
        }
    }
}
