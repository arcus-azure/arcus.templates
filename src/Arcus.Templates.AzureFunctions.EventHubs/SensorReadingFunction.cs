using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Azure.Messaging.EventHubs;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
 
namespace Arcus.Templates.AzureFunctions.EventHubs
{
    public class SensorReadingFunction
    {
        private readonly string _jobId = Guid.NewGuid().ToString();
        private readonly IAzureEventHubsMessageRouter _messageRouter;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SensorReadingFunction"/> class.
        /// </summary>
        /// <param name="messageRouter">The message router instance to route the Azure EventHubs events through the sensor-reading processing.</param
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageRouter"/> is <c>null</c>.</exception>
        public SensorReadingFunction(IAzureEventHubsMessageRouter messageRouter)
        {
            ArgumentNullException.ThrowIfNull(messageRouter);
            _messageRouter = messageRouter;
        }
        
        /// <summary>
        /// Processes Azure EventHubs <paramref name="events"/>.
        /// </summary>
        /// <param name="events">The incoming events on the Azure EventHubs instance.</param>
        /// <param name="executionContext">The execution context for this Azure Functions instance.</param>
        [Function("sensor-reading")]
        public async Task Run(
            [EventHubTrigger("sensors", Connection = "EventHubsConnectionString")] EventData[] events,
            FunctionContext executionContext)
        {
            foreach (EventData @event in events)
            {
                ILogger logger = executionContext.GetLogger<SensorReadingFunction>();
                logger.LogInformation("Azure EventHubs function triggered with Message ID {MessageId}", @event.MessageId);

                var client = executionContext.InstanceServices.GetRequiredService<TelemetryClient>();
                (string transactionId, string operationParentId) = @event.Properties.GetTraceParent();
                AzureEventHubsMessageContext messageContext = @event.GetMessageContext("sensor-reading.servicebus.windows.net", "sensors", "$Default", _jobId);

                using MessageCorrelationResult result = MessageCorrelationResult.Create(client, transactionId, operationParentId);
                await _messageRouter.RouteMessageAsync(@event, messageContext, result.CorrelationInfo, CancellationToken.None);
            }
        }
    }
}
