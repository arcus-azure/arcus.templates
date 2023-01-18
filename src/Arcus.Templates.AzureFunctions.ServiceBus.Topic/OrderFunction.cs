using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Templates.AzureFunctions.ServiceBus.Topic.Model;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Arcus.Observability.Correlation;
#if Isolated
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
#endif
#if InProcess
using Arcus.Messaging.AzureFunctions.ServiceBus;
using Microsoft.Azure.WebJobs;
#endif
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
#if InProcess
        private readonly AzureFunctionsInProcessMessageCorrelation _messageCorrelation;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFunction" /> class.
        /// </summary>
        /// <param name="messageRouter">The message router instance to route the Azure Service Bus topic messages through the order processing.</param>
        /// <param name="messageCorrelation">The message correlation instance to W3C correlate the Azure Service Bus topic messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageRouter"/> or the <paramref name="messageCorrelation"/> is <c>null</c>.</exception>
        public OrderFunction(
            IAzureServiceBusMessageRouter messageRouter, 
            AzureFunctionsInProcessMessageCorrelation messageCorrelation)
        {
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires a message router instance to route the incoming Azure Service Bus topic message through the order processing");
            Guard.NotNull(messageCorrelation, nameof(messageCorrelation), "Requires a message correlation instance to W3C correlate incoming Azure Service Bus topic messages");

            _messageRouter = messageRouter;
            _messageCorrelation = messageCorrelation;
            _jobId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Process an Azure Service Bus topic <paramref name="message"/> as an <see cref="Order"/>.
        /// </summary>
        /// <param name="message">The incoming message on the Azure Service Bus topic, representing an <see cref="Order"/>.</param>
        /// <param name="log">The logger instance to write informational messages during the message processing.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        [FunctionName("order-processing")]
        public async Task Run(
            [ServiceBusTrigger("order-topic", "order-subscription",  Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message,
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {message.MessageId}");
            
            AzureServiceBusMessageContext messageContext = message.GetMessageContext(_jobId);
            using (MessageCorrelationResult result = _messageCorrelation.CorrelateMessage(message))
            {
                await _messageRouter.RouteMessageAsync(message, messageContext, result.CorrelationInfo, cancellationToken);
            }
        }
#endif
#if Isolated
        
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
            logger.LogInformation($"C# ServiceBus topic trigger function processed message: {message.MessageId}");
            
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
            
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromBytes(messageBody),
                messageId: executionContext.BindingContext.BindingData["MessageId"]?.ToString(),
                correlationId: executionContext.BindingContext.BindingData["CorrelationId"]?.ToString(),
                properties: applicationProperties);
            
            return message;
        }
#endif
    }
}
