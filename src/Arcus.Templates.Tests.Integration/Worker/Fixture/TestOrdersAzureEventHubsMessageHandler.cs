using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.Worker.Fixture
{
    public class TestOrdersAzureEventHubsMessageHandler : IAzureEventHubsMessageHandler<Order>
    {
        private readonly ILogger<TestOrdersAzureEventHubsMessageHandler> _logger;
        private readonly IEventGridPublisher _eventGridPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestOrdersAzureEventHubsMessageHandler" /> class.
        /// </summary>
        public TestOrdersAzureEventHubsMessageHandler(IConfiguration configuration, ILogger<TestOrdersAzureEventHubsMessageHandler> logger)
        {
            _logger = logger;
            var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
            var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");

            _eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(eventGridTopic)
                    .UsingAuthenticationKey(eventGridKey)
                    .Build();
        }

        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     Thrown when the <paramref name="message" />, <paramref name="messageContext" />, or the <paramref name="correlationInfo" /> is <c>null</c>.
        /// </exception>
        public async Task ProcessMessageAsync(
            Order message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing order {OrderId} for {OrderAmount} units of {OrderArticle}",
                message.Id, message.Amount, message.ArticleNumber);

            await PublishEventToEventGridAsync(message, correlationInfo);

            _logger.LogInformation("Order {OrderId} processed", message.Id);
        }

        private async Task PublishEventToEventGridAsync(Order orderMessage, MessageCorrelationInfo correlationInfo)
        {
            var eventData = new OrderCreatedEventData(
                orderMessage.Id,
                orderMessage.Amount,
                orderMessage.ArticleNumber,
                $"{orderMessage.Customer.FirstName} {orderMessage.Customer.LastName}",
                correlationInfo);

            var orderCreatedEvent = new CloudEvent(
                CloudEventsSpecVersion.V1_0,
                "OrderCreatedEvent",
                new Uri("http://test-host"),
                correlationInfo.OperationId,
                DateTime.UtcNow)
            {
                Data = eventData,
                DataContentType = new ContentType("application/json")
            };

            await _eventGridPublisher.PublishAsync(orderCreatedEvent);

            _logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}
