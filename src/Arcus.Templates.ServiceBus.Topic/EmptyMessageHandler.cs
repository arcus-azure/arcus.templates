using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.ServiceBus.Topic
{
    /// <summary>
    /// Empty implementation of the <see cref="AzureServiceBusMessagePump"/>, using an <see cref="EmptyMessage"/> as event message.
    /// </summary>
    public class EmptyMessageHandler : IAzureServiceBusMessageHandler<EmptyMessage>
    {
        private readonly ILogger<EmptyMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyMessageHandler"/> class.
        /// </summary>
        /// <param name="logger">Logger to write telemetry to</param>
        public EmptyMessageHandler(ILogger<EmptyMessageHandler> logger)
        {
            _logger = logger;
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
            EmptyMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("Processing message {message}...", message);

            // Process message.

            _logger.LogInformation("Message {message} processed!", message);
        }
    }
}
