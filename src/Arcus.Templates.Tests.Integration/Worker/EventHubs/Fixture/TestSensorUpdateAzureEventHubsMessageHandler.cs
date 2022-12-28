using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Azure.Messaging.EventGrid;
using Azure;
using Azure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    public class TestSensorUpdateAzureEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorUpdate>
    {
        private readonly ILogger _logger;
        private readonly EventGridPublisherClient _eventGridPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSensorUpdateAzureEventHubsMessageHandler" /> class.
        /// </summary>
        public TestSensorUpdateAzureEventHubsMessageHandler(IConfiguration configuration, ILogger<TestSensorUpdateAzureEventHubsMessageHandler> logger)
        {
            var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
            var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");
            _eventGridPublisher = new EventGridPublisherClient(new Uri(eventGridTopic), new AzureKeyCredential(eventGridKey));
            _logger = logger;
        }

        public async Task ProcessMessageAsync(
            SensorUpdate message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing sensor reading {SensorId} for status {SensorStatus} on {Timestamp}", message.SensorId, message.SensorStatus, message.Timestamp);

            await PublishEventToEventGridAsync(message, correlationInfo);

            _logger.LogInformation("Sensor {SensorId} processed", message.SensorId);
        }

        private async Task PublishEventToEventGridAsync(SensorUpdate message, MessageCorrelationInfo correlationInfo)
        {
            var eventData = new SensorUpdateEventData
            {
                SensorId = message.SensorId,
                SensorStatus = message.SensorStatus,
                Timestamp = message.Timestamp,
                CorrelationInfo = correlationInfo
            };

            var orderCreatedEvent = new CloudEvent(
                "http://test-host",
                "SensorReadEvent",
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
