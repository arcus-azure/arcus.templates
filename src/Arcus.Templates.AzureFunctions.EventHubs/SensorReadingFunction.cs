using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Azure.Messaging.EventHubs;
using GuardNet;
#if InProcess
using Arcus.Messaging.AzureFunctions.EventHubs;
using Microsoft.Azure.WebJobs;
using Arcus.Observability.Correlation;
#endif
#if Isolated
using Microsoft.Azure.Functions.Worker; 
#endif
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.AzureFunctions.EventHubs
{
    public class SensorReadingFunction
    {
        private readonly IAzureEventHubsMessageRouter _messageRouter;
#if InProcess
        private readonly AzureFunctionsInProcessMessageCorrelation _messageCorrelation;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SensorReadingFunction"/> class.
        /// </summary>
        /// <param name="messageRouter">The message router instance to route the Azure EventHubs events through the sensor-reading processing.</param
        /// <param name="messageCorrelation">The message correlation instance to W3C correlate the Azure EventHubs events.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageRouter"/> or the <paramref name="messageCorrelation"/> is <c>null</c>.</exception>
        public SensorReadingFunction(
            IAzureEventHubsMessageRouter messageRouter,
            AzureFunctionsInProcessMessageCorrelation messageCorrelation)
        {
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires a message router instance to route incoming Azure EventHubs events through the sensor-reading processing");
            Guard.NotNull(messageCorrelation, nameof(messageCorrelation), "Requires a message correlation instance to W3C correlate incoming Azure EventHubs events");
            
            _messageRouter = messageRouter;
            _messageCorrelation = messageCorrelation;
        }
        
        /// <summary>
        /// Processes Azure EventHubs <paramref name="events"/>.
        /// </summary>
        /// <param name="events">The incoming events on the Azure EventHubs instance.</param>
        /// <param name="log">The logger instance to write informational messages during the message processing.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        [FunctionName("sensor-reading")]
        public async Task Run(
            [EventHubTrigger("sensors", Connection = "EventHubsConnectionString")] EventData[] events,
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation("Azure EventHubs function triggered with {Length} events", events.Length);
            foreach (EventData message in events)
            {
                AzureEventHubsMessageContext messageContext = message.GetMessageContext("sensor-reading.servicebus.windows.net", "sensors");
                using (MessageCorrelationResult result = _messageCorrelation.CorrelateMessage(message))
                {
                    await _messageRouter.RouteMessageAsync(message, messageContext, result.CorrelationInfo, cancellationToken); 
                }
            }
        }
#endif
#if Isolated
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SensorReadingFunction"/> class.
        /// </summary>
        /// <param name="messageRouter">The message router instance to route the Azure EventHubs events through the sensor-reading processing.</param
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageRouter"/> is <c>null</c>.</exception>
        public SensorReadingFunction(IAzureEventHubsMessageRouter messageRouter)
        {
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires a message router instance to route incoming Azure EventHubs events through the sensor-reading processing");
            _messageRouter = messageRouter;
        }
        
        /// <summary>
        /// Processes Azure EventHubs <paramref name="events"/>.
        /// </summary>
        /// <param name="events">The incoming events on the Azure EventHubs instance.</param>
        /// <param name="propertiesArray">The array containing the set of properties for each received Azure EventHubs event.</param>
        /// <param name="executionContext">The execution context for this Azure Functions instance.</param>
        [Function("sensor-reading")]
        public async Task Run(
            [EventHubTrigger("sensors", Connection = "EventHubsConnectionString")] string[] events,
            Dictionary<string, JsonElement>[] propertiesArray,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger<SensorReadingFunction>();
            logger.LogInformation("Azure EventHubs function triggered with {Length} events", events.Length);
            
            for (var index = 0; index < events.Length; index++)
            {
                string message = events[index];
                Dictionary<string, JsonElement> properties = propertiesArray[index];
                EventData data = CreateEventData(message, properties);
                
                AzureEventHubsMessageContext messageContext = data.GetMessageContext("sensor-reading.servicebus.windows.net", "sensors");
                using (MessageCorrelationResult result = executionContext.GetCorrelationInfo(properties))
                {
                    await _messageRouter.RouteMessageAsync(data, messageContext, result.CorrelationInfo, CancellationToken.None);
                }
            }
        }
        
        private static EventData CreateEventData(string message, IDictionary<string, JsonElement> properties)
        {
            var data = new EventData(message);
            foreach (KeyValuePair<string, JsonElement> property in properties)
            {
                data.Properties.Add(property.Key, property.Value.GetString());
            }
            
            return data;
        }
#endif
    }
}
