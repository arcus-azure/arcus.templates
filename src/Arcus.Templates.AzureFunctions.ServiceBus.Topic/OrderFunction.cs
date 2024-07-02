using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Templates.AzureFunctions.ServiceBus.Topic.Model;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
 
namespace Arcus.Templates.AzureFunctions.ServiceBus.Topic
{
    /// <summary>
    /// Represents an Azure Service Bus topic trigger that processes <see cref="Order"/> messages.
    /// </summary>
    public class OrderFunction
    {
        private readonly string _jobId;
        private readonly IAzureServiceBusMessageRouter _messageRouter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFunction" /> class.
        /// </summary>
        /// <param name="messageRouter">The message router instance to route the Azure Service Bus topic messages through the order processing.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageRouter"/> is <c>null</c>.</exception>
        public OrderFunction(IAzureServiceBusMessageRouter messageRouter)
        {
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires a message router instance to route the incoming Azure Service Bus topic message through the order processing");
            
            _messageRouter = messageRouter;
            _jobId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Process an Azure Service Bus topic <paramref name="message"/> as an <see cref="Order"/>.
        /// </summary>
        /// <param name="message">The incoming message on the Azure Service Bus topic, representing an <see cref="Order"/>.</param>
        /// <param name="executionContext">The execution context for this Azure Functions instance.</param>
        [Function("order-processing")]
        public async Task Run(
            [ServiceBusTrigger("order-topic", "order-subscription", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger<ILogger<OrderFunction>>();
            logger.LogInformation("C# ServiceBus topic trigger function processed message: {MessageId}", message.MessageId);
            
            AzureServiceBusMessageContext messageContext = message.GetMessageContext(_jobId);
            using (MessageCorrelationResult result = executionContext.GetCorrelationInfo())
            {
                await _messageRouter.RouteMessageAsync(message, messageContext, result.CorrelationInfo, CancellationToken.None);
            }
        }
    }
}
