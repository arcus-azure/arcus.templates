using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.Worker.Fixture
{
    public class OrdersMessagePump : AzureServiceBusMessagePump<Order>
    {
        private readonly IEventGridPublisher _eventGridPublisher;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        public OrdersMessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<OrdersMessagePump> logger)
            : base(configuration, serviceProvider, logger)
        {
            var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
            var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");

            _eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(eventGridTopic)
                    .UsingAuthenticationKey(eventGridKey)
                    .Build();
        }

        /// <inheritdoc />
        protected override async Task ProcessMessageAsync(
            Order orderMessage,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Logger.LogInformation(
                "Processing order {OrderId} for {OrderAmount} units of {OrderArticle}",
                orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber);

            await PublishEventToEventGridAsync(orderMessage, correlationInfo.OperationId, correlationInfo);

            Logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
        }

        private async Task PublishEventToEventGridAsync(Order orderMessage, string operationId, MessageCorrelationInfo correlationInfo)
        {
            var eventData = new OrderCreatedEventData(
                orderMessage.Id,
                orderMessage.Amount,
                orderMessage.ArticleNumber,
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

            Logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}
