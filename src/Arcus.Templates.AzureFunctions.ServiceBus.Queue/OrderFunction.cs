using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Templates.AzureFunctions.ServiceBus.Queue.Model;
using Azure.Messaging.ServiceBus;
using GuardNet;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
 
namespace Arcus.Templates.AzureFunctions.ServiceBus.Queue
{
    /// <summary>
    /// Represents an Azure Service Bus queue trigger that processes <see cref="Order"/> messages.
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
        /// Process an Azure Service Bus queue <paramref name="messageBody"/> as an <see cref="Order"/>.
        /// </summary>
        /// <param name="messageBody">The incoming message on the Azure Service Bus queue, representing an <see cref="Order"/>.</param>
        /// <param name="executionContext">The execution context for this Azure Functions instance.</param>
        [Function("order-processing")]
        public async Task Run(
            [ServiceBusTrigger("orders", Connection = "ServiceBusConnectionString")] byte[] messageBody,
            FunctionContext executionContext)
        {
            ServiceBusReceivedMessage message = ConvertToServiceBusMessage(messageBody, executionContext);
            var logger = executionContext.GetLogger<ILogger<OrderFunction>>();
            logger.LogInformation("C# ServiceBus queue trigger function processed message: {MessageId}", message.MessageId);
            
            AzureServiceBusMessageContext messageContext = message.GetMessageContext(_jobId);
            using (MessageCorrelationResult result = executionContext.GetCorrelationInfo())
            {
                await _messageRouter.RouteMessageAsync(message, messageContext, result.CorrelationInfo, CancellationToken.None);
            }
        }
        
        private static ServiceBusReceivedMessage ConvertToServiceBusMessage(byte[] messageBody, FunctionContext executionContext)
        {
            var applicationProperties = new Dictionary<string, object>();
            if (executionContext.BindingContext.BindingData.TryGetValue("ApplicationProperties", out object applicationPropertiesObj))
            {
                var json = applicationPropertiesObj.ToString();
                applicationProperties = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            }
            
            executionContext.BindingContext.BindingData.TryGetValue("CorrelationId", out object correlationId);
            
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromBytes(messageBody),
                messageId: executionContext.BindingContext.BindingData["MessageId"]?.ToString(),
                correlationId: correlationId?.ToString(),
                properties: applicationProperties);

            return message;
        }
    }
}
