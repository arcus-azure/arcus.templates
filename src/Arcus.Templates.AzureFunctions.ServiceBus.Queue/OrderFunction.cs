using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Templates.AzureFunctions.ServiceBus.Queue.Model;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.AzureFunctions.ServiceBus.Queue
{
    /// <summary>
    /// Represents an Azure Service Bus queue trigger that processes <see cref="Order"/> messages.
    /// </summary>
    public class OrderFunction
    {
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        private readonly string _jobId;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFunction" /> class.
        /// </summary>
        /// <param name="messageRouter">The message router instance to route the Azure Service Bus queue messages through the order processing.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageRouter"/> is <c>null</c>.</exception>
        public OrderFunction(IAzureServiceBusMessageRouter messageRouter)
        {
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires an message router instance to route the incoming Azure Service Bus queue message through the order processing");

            _messageRouter = messageRouter;
            _jobId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Process an Azure Service Bus queue <paramref name="message"/> as an <see cref="Order"/>.
        /// </summary>
        /// <param name="message">The incoming message on the Azure Service Bus queue, representing an <see cref="Order"/>.</param>
        /// <param name="log">The logger instance to write informational messages during the message processing.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        [FunctionName("order-processing")]
        public async Task Run(
            [ServiceBusTrigger("orders", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message, 
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {message.MessageId}");

            AzureServiceBusMessageContext messageContext = message.GetMessageContext(_jobId);
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();
            await _messageRouter.RouteMessageAsync(message, messageContext, correlationInfo, cancellationToken);
        }
    }
}
