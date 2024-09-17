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
            ArgumentNullException.ThrowIfNull(messageRouter);

            _messageRouter = messageRouter;
            _jobId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Process an Azure Service Bus topic <paramref name="messageBody"/> as an <see cref="Order"/>.
        /// </summary>
        /// <param name="messageBody">The incoming message on the Azure Service Bus topic, representing an <see cref="Order"/>.</param>
        /// <param name="executionContext">The execution context for this Azure Functions instance.</param>
        [Function("order-processing")]
        public async Task Run(
            [ServiceBusTrigger("order-topic", "order-subscription", Connection = "ServiceBusConnectionString")] byte[] messageBody,
            FunctionContext executionContext)
        {
            ServiceBusReceivedMessage message = ConvertToServiceBusMessage(messageBody, executionContext);
            var logger = executionContext.GetLogger<ILogger<OrderFunction>>();
            logger.LogInformation("C# ServiceBus topic trigger function processed message: {MessageId}", message.MessageId);
            
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
