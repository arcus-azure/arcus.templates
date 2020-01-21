using Arcus.EventGrid.Contracts;
using Arcus.Messaging.Abstractions;
using Newtonsoft.Json;

namespace Arcus.Templates.Tests.Integration.Worker.Fixture 
{
    public class OrderCreatedEvent : EventGridEvent<OrderCreatedEventData>
    {
        private const string DefaultDataVersion = "1";
        private const string DefaultEventType = "Arcus.Samples.Orders.OrderCreated";

        public OrderCreatedEvent(string eventId, string orderId, int amount, string articleNumber, MessageCorrelationInfo correlationInfo)
            : base(eventId, subject: "order-created",
                   new OrderCreatedEventData(orderId, amount, articleNumber, correlationInfo),
                   DefaultDataVersion,
                   DefaultEventType)
        {
        }

        [JsonConstructor]
        private OrderCreatedEvent()
        {
        }
    }
}