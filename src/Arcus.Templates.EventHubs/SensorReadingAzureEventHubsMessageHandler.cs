using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Templates.EventHubs.Model;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.EventHubs
{
    /// <summary>
    /// Represents an <see cref="IAzureEventHubsMessageHandler{TMessage}"/> implementation that processes <see cref="SensorReading"/> messages.
    /// </summary>
    public class SensorReadingAzureEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorReading>
    {
        private readonly ILogger<SensorReadingAzureEventHubsMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorReadingAzureEventHubsMessageHandler" /> class.
        /// </summary>
        /// <param name="logger">The logger instance to write informational message during the order processing.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public SensorReadingAzureEventHubsMessageHandler(ILogger<SensorReadingAzureEventHubsMessageHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
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
        public Task ProcessMessageAsync(
            SensorReading message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sensor reading processed!");

            return Task.CompletedTask;
        }
    }
}