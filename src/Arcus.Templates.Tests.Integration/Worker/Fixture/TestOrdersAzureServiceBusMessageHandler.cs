using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CloudEvent = Azure.Messaging.CloudEvent;

namespace Arcus.Templates.Tests.Integration.Worker.Fixture
{
    public class TestOrdersAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly ILogger<TestOrdersAzureServiceBusMessageHandler> _logger;
        private readonly EventGridPublisherClient _eventGridPublisher;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="logger">Logger to write telemetry to</param>
        public TestOrdersAzureServiceBusMessageHandler(IConfiguration configuration, ILogger<TestOrdersAzureServiceBusMessageHandler> logger)
        {
            _logger = logger;
            var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
            var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");

            _eventGridPublisher = new EventGridPublisherClient(new Uri(eventGridTopic), new AzureKeyCredential(eventGridKey));
        }

        /// <summary>Process a new message that was received</summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing order {OrderId} for {OrderAmount} units of {OrderArticle}",
                message.Id, message.Amount, message.ArticleNumber);

            await PublishEventToEventGridAsync(message, correlationInfo.OperationId, correlationInfo);

            _logger.LogInformation("Order {OrderId} processed", message.Id);
        }

        private async Task PublishEventToEventGridAsync(Order orderMessage, string operationId, MessageCorrelationInfo correlationInfo)
        {
            var eventData = new OrderCreatedEventData(
                orderMessage.Id,
                orderMessage.Amount,
                orderMessage.ArticleNumber,
                $"{orderMessage.Customer.FirstName} {orderMessage.Customer.LastName}",
                correlationInfo);

            var orderCreatedEvent = new CloudEvent(
                "http://test-host",
                "OrderCreatedEvent",
                jsonSerializableData: eventData)
            {
                Id = correlationInfo.OperationId,
                Time = DateTimeOffset.UtcNow
            };

            await _eventGridPublisher.SendEventAsync(orderCreatedEvent);

            _logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}
