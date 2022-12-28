using Arcus.Messaging.Abstractions;
using Azure.Messaging.EventGrid;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture
{
    public class OrderCreatedEvent : EventGridEvent
    {
        private const string DefaultDataVersion = "1";
        private const string DefaultEventType = "Arcus.Samples.Orders.OrderCreated";

        public OrderCreatedEvent(string eventId, string orderId, int amount, string articleNumber, string customerName, MessageCorrelationInfo correlationInfo)
            : base($"customer/{customerName}", DefaultEventType, DefaultDataVersion, new OrderCreatedEventData(orderId, amount, articleNumber, customerName, correlationInfo))
        {
            Id = eventId;
        }
    }
}