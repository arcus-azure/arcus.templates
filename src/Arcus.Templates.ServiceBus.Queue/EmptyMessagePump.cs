using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.ServiceBus.Queue
{
    /// <summary>
    /// Empty implementation of the <see cref="AzureServiceBusMessagePump{TMessage}"/>, using an <see cref="object"/> as event message.
    /// </summary>
    public class EmptyMessagePump : AzureServiceBusMessagePump<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyMessagePump"/> class.
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        public EmptyMessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<EmptyMessagePump> logger) 
            : base(configuration, serviceProvider, logger)
        {
        }

        /// <inheritdoc />
        protected override async Task ProcessMessageAsync(
            object message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("Processing message...");

            // Process message.

            Logger.LogInformation("Message processed!");
        }
    }
}
