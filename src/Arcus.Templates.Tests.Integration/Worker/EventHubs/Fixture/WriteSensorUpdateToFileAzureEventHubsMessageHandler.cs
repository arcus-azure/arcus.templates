using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    public class WriteSensorUpdateToFileAzureEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorUpdate>
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSensorUpdateToFileAzureEventHubsMessageHandler" /> class.
        /// </summary>
        public WriteSensorUpdateToFileAzureEventHubsMessageHandler(ILogger<WriteSensorUpdateToFileAzureEventHubsMessageHandler> logger)
        {
            _logger = logger;
        }

        public async Task ProcessMessageAsync(
            SensorUpdate message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing sensor reading {SensorId} for status {SensorStatus} on {Timestamp}", message.SensorId, message.SensorStatus, message.Timestamp);

            var eventData = new SensorUpdateEventData
            {
                SensorId = message.SensorId,
                SensorStatus = message.SensorStatus,
                Timestamp = message.Timestamp,
                CorrelationInfo = correlationInfo
            };

            string json = JsonSerializer.Serialize(eventData);
            string currentDirPath = Directory.GetCurrentDirectory();

            var fileName = $"{correlationInfo.TransactionId}.json";
            _logger.LogTrace("Processed message by writing on disk: {FileName}", fileName);
            string filePath = Path.Combine(currentDirPath, fileName);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogInformation("Sensor {SensorId} processed to: {FilePath}", message.SensorId, filePath);
        }
    }
}