using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Azure.Messaging.EventHubs;
#if InProcess
using Microsoft.Azure.WebJobs;
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

        public SensorReadingFunction(IAzureEventHubsMessageRouter messageRouter)
        {
            _messageRouter = messageRouter;
        }

#if InProcess
        [FunctionName("sensor-reading")]
        public async Task Run(
            [EventHubTrigger("sensors", Connection = "EventHubsConnectionString")] EventData[] events,
            ILogger log,
            CancellationToken cancellationToken)
        {
            foreach (EventData message in events)
            {
                log.LogInformation($"First Event Hubs triggered message: {message.MessageId}");

                var messageContext = AzureEventHubsMessageContext.CreateFrom(message, "sensor-reading.servicebus.windows.net", "$Default", "sensors");
                MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();
                await _messageRouter.RouteMessageAsync(message, messageContext, correlationInfo, cancellationToken); 
            }
        }
#endif
#if Isolated
        [Function("sensor-reading")]
        public async Task Run(
            [EventHubTrigger("sensors", Connection = "EventHubsConnectionString")] string[] messages,
            Dictionary<string, JsonElement>[] propertiesArray,
            FunctionContext context)
        {
            ILogger logger = context.GetLogger<SensorReadingFunction>();
            logger.LogInformation($"Event Hubs triggered with {messages.Length} messages");

            for (var index = 0; index < messages.Length; index++)
            {
                string message = messages[index];
                Dictionary<string, JsonElement> properties = propertiesArray[index];
                EventData data = CreateEventData(message, properties);

                var messageContext = AzureEventHubsMessageContext.CreateFrom(data, "sensor-reading.servicebus.windows.net", "$Default", "sensors");
                MessageCorrelationInfo correlationInfo = data.GetCorrelationInfo();

                await _messageRouter.RouteMessageAsync(data, messageContext, correlationInfo, CancellationToken.None);
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
