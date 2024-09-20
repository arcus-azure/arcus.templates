using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Templates.AzureFunctions.ServiceBus.Topic.Model;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.AzureFunctions.ServiceBus.Topic
{
    /// <summary>
    /// Represents an <see cref="IAzureServiceBusMessageHandler{TMessage}"/> implementation that processes <see cref="Order"/> messages.
    /// </summary>
    public class OrdersAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly ILogger<OrdersAzureServiceBusMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersAzureServiceBusMessageHandler" /> class.
        /// </summary>
        /// <param name="logger">The logger instance to write informational message during the order processing.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public OrdersAzureServiceBusMessageHandler(ILogger<OrdersAzureServiceBusMessageHandler> logger)
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
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Order processed!");

            return Task.CompletedTask;
        }
    }
}