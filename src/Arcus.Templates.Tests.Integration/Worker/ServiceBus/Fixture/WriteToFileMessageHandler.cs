using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture
{
    public class WriteToFileMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly ILogger<WriteToFileMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteToFileMessageHandler" /> class.
        /// </summary>
        public WriteToFileMessageHandler(ILogger<WriteToFileMessageHandler> logger)
        {
            _logger = logger;
        }

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

            var fileName = $"{correlationInfo.TransactionId}.json";
            _logger.LogTrace("Processed message by writing on disk: {FileName}", fileName);
            File.WriteAllText(Path.Combine(currentDirPath, fileName), json);

            return Task.CompletedTask;
        }
    }
}
