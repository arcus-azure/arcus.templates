using System;
using System.IO;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Xunit;
using Xunit.Abstractions;
using TestConfig = Arcus.Templates.Tests.Integration.Fixture.TestConfig;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture
{
    public class TestServiceBusMessagePumpService : IMessagingService
    {
        private readonly ServiceBusEntityType _entityType;
        private readonly TestConfig _configuration;
        private readonly DirectoryInfo _projectDirectory;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceBusMessagePumpService" /> class.
        /// </summary>
        public TestServiceBusMessagePumpService(
            ServiceBusEntityType entityType,
            TestConfig configuration,
            DirectoryInfo projectDirectory,
            ITestOutputHelper outputWriter)
        {
            _entityType = entityType;
            _configuration = configuration;
            _projectDirectory = projectDirectory;
            _logger = new XunitTestLogger(outputWriter);
        }

        public async Task SimulateMessageProcessingAsync()
        {
            var traceParent = TraceParent.Generate();
            Order order = GenerateOrder();
            await ProduceMessageAsync(order, traceParent);

            OrderCreatedEventData orderCreatedEventData = await ConsumeMessageAsync(traceParent);
            Assert.NotNull(orderCreatedEventData);
            Assert.NotNull(orderCreatedEventData.CorrelationInfo);
            Assert.Equal(order.Id, orderCreatedEventData.Id);
            Assert.Equal(order.Amount, orderCreatedEventData.Amount);
            Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
            Assert.Equal(traceParent.TransactionId, orderCreatedEventData.CorrelationInfo.TransactionId);
            Assert.Equal(traceParent.OperationParentId, orderCreatedEventData.CorrelationInfo.OperationParentId);
            Assert.NotEmpty(orderCreatedEventData.CorrelationInfo.CycleId);
        }

        private static Order GenerateOrder()
        {
            var customerGenerator = new Faker<Customer>()
               .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName())
               .RuleFor(u => u.LastName, (f, u) => f.Name.LastName());

            var orderGenerator = new Faker<Order>()
                .RuleFor(u => u.Customer, () => customerGenerator)
                .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
                .RuleFor(u => u.Amount, f => f.Random.Int())
                .RuleFor(u => u.ArticleNumber, f => f.Commerce.Product());

            return orderGenerator.Generate();
        }

        private async Task ProduceMessageAsync(Order order, TraceParent traceParent)
        {
            _logger.LogTrace("Produces a message with transaction ID: {TransactionId}", traceParent.TransactionId);

            var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(order));
            message.ApplicationProperties["Diagnostic-Id"] = traceParent.DiagnosticId;

            string connectionString = _configuration.GetServiceBusConnectionString(_entityType);
            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(connectionString);
            await using (var client = new ServiceBusClient(connectionString))
            {
                ServiceBusSender messageSender = client.CreateSender(connectionStringProperties.EntityPath);

                try
                {
                    await messageSender.SendMessageAsync(message);
                }
                finally
                {
                    await messageSender.CloseAsync();
                }
            }
        }

        private async Task<OrderCreatedEventData> ConsumeMessageAsync(TraceParent traceParent)
        {
            _logger.LogTrace("Consumes a message with transaction ID: {TransactionId}", traceParent.TransactionId);

            FileInfo[] foundFiles =
                await Poll.Target(() => Task.FromResult(_projectDirectory.GetFiles(traceParent.TransactionId + ".json", SearchOption.AllDirectories)))
                          .Until(files => files.Length > 0)
                          .Every(TimeSpan.FromMilliseconds(200))
                          .Timeout(TimeSpan.FromMinutes(1))
                          .FailWith("Failed to retrieve the necessary produced message from the temporary project created from the worker project template, " +
                                    "please check whether the injected message handler was correct and if the created project correctly receives the message");

            FileInfo found = Assert.Single(foundFiles);
            string json = await File.ReadAllTextAsync(found.FullName);
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new MessageCorrelationInfoJsonConverter());

            return JsonConvert.DeserializeObject<OrderCreatedEventData>(json, settings);
        }
    }
}
