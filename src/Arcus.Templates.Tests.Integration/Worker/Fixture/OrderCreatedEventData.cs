using Arcus.Messaging.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Fixture 
{
    public class OrderCreatedEventData
    {
        public OrderCreatedEventData(string id, int amount, string articleNumber, string customerName, MessageCorrelationInfo correlationInfo)
        {
            Id = id;
            Amount = amount;
            ArticleNumber = articleNumber;
            CustomerName = customerName;
            CorrelationInfo = correlationInfo;
        }

        public string Id { get; set; }
        public int Amount { get; set; }
        public string ArticleNumber { get; set; }
        public string CustomerName { get; set; }
        public MessageCorrelationInfo CorrelationInfo { get; set; }
    }
}