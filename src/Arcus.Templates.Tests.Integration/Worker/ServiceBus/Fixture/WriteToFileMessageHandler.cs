using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture
{
    public class WriteToFileMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        public Task ProcessMessageAsync(
            Order orderMessage,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var eventData = new OrderCreatedEventData(
                orderMessage.Id,
                orderMessage.Amount,
                orderMessage.ArticleNumber,
                $"{orderMessage.Customer.FirstName} {orderMessage.Customer.LastName}",
                correlationInfo);

            string json = JsonSerializer.Serialize(eventData);
            string currentDirPath = Directory.GetCurrentDirectory();
            File.WriteAllText(Path.Combine(currentDirPath, $"{correlationInfo.TransactionId}.json"), json);

            return Task.CompletedTask;
        }
    }
}
