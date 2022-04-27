using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.Worker.Fixture
{
    /// <summary>
    /// Represents an <see cref="IAzureServiceBusMessageHandler{TMessage}"/> implementation that processes <see cref="Order"/> messages.
    /// </summary>
    public class TestOrderAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly ILogger<TestOrderAzureServiceBusMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestOrderAzureServiceBusMessageHandler" /> class.
        /// </summary>
        /// <param name="logger">The logger instance to write informational message during the order processing.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public TestOrderAzureServiceBusMessageHandler(
            IConfiguration configuration,
            ILogger<TestOrderAzureServiceBusMessageHandler> logger)
        {
            Guard.NotNull(logger, nameof(logger), "Requires an logger instance to write informational messages during the order processing");
             var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
            var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");

            _eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(eventGridTopic)
                    .UsingAuthenticationKey(eventGridKey)
                    .Build();

            _logger = logger;
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
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("Processing order {OrderId} for {OrderAmount} units of {OrderArticle}", message.Id, message.Amount, message.ArticleNumber);

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
                CloudEventsSpecVersion.V1_0,
                "OrderCreatedEvent",
                new Uri("http://test-host"),
                operationId,
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
