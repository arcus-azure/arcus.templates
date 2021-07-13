using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.MessageHandling;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.AzureFunctions.ServiceBus.Queue
{
    /// <summary>
    /// Represents an Azure Function that processes a Azure Service Bus message from a queue.
    /// </summary>
    public class ServiceBusQueueFunction
    {
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusQueueFunction"/> class.
        /// </summary>
        /// <param name="messageRouter">The router instance to route incoming Azure Service Bus Queue messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageRouter"/> is <c>null</c>.</exception>
        public ServiceBusQueueFunction(IAzureServiceBusMessageRouter messageRouter)
        {
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires a message router instance to route incoming Azure Service Bus Queue messages through registered message handlers");
            _messageRouter = messageRouter;
        }

        /// <summary>
        /// Processes a Azure Service Bus Queue message.
        /// </summary>
        /// <param name="message">The incoming message from the Azure Service Bus Queue.</param>
        /// <param name="messageReceiver">The instance that received the Azure Service Bus Queue message.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the processing of the Azure Service Bus Queue message.</param>
        /// <param name="cancellationToken">The token to cancel the processing of the received Azure Service Bus Queue message.</param>
        [FunctionName("servicebus-queue")]
        public async Task Run(
            [ServiceBusTrigger("order", Connection = "Arcus:ServiceBus:ConnectionString")] 
            Message message,
            MessageReceiver messageReceiver,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            logger.LogInformation($"C# ServiceBus queue trigger function processed message: {message.MessageId}");

            var context = new AzureServiceBusMessageContext(message.MessageId, message.SystemProperties, message.UserProperties);
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();
            await _messageRouter.ProcessMessageAsync(messageReceiver, message, context, correlationInfo, cancellationToken);
        }
    }
}
