using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    public class TestEventHubsMessagePumpService : IMessagingService
    {
        private readonly TestConfig _configuration;
        private readonly ILogger _logger;

        private TestServiceBusMessageEventConsumer _serviceBusMessageEventConsumer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestEventHubsMessagePumpService" /> class.
        /// </summary>
        public TestEventHubsMessagePumpService(
            TestConfig configuration,
            ITestOutputHelper outputWriter)
        {
            _configuration = configuration;
            _logger = new XunitTestLogger(outputWriter);
        }

        public async Task StartAsync()
        {
            if (_serviceBusMessageEventConsumer is null)
            {
                _serviceBusMessageEventConsumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_configuration, _logger);
            }
            else
            {
                throw new InvalidOperationException("Service is already started!");
            }
        }

        public async Task SimulateMessageProcessingAsync()
        {
            if (_serviceBusMessageEventConsumer is null)
            {
                throw new InvalidOperationException(
                    "Cannot simulate the message pump because the service is not yet started; please start this service before simulating");
            }

            var operationId = $"operation-{Guid.NewGuid()}";
            var transactionId = $"transaction-{Guid.NewGuid()}";
            SensorUpdate update = GenerateSensorReading();
            await ProduceEventAsync(update, operationId, transactionId);

            var sensorReadEventData = _serviceBusMessageEventConsumer.ConsumeEvent<SensorUpdateEventData>(operationId);
            Assert.NotNull(sensorReadEventData);
            Assert.NotNull(sensorReadEventData.CorrelationInfo);
            Assert.Equal(update.SensorId, sensorReadEventData.SensorId);
            Assert.Equal(update.SensorStatus, sensorReadEventData.SensorStatus);
            Assert.Equal(update.Timestamp, sensorReadEventData.Timestamp);
            Assert.Equal(transactionId, sensorReadEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, sensorReadEventData.CorrelationInfo.OperationId);
            Assert.NotEmpty(sensorReadEventData.CorrelationInfo.CycleId);
        }

        private static SensorUpdate GenerateSensorReading()
        {
            return new Faker<SensorUpdate>()
                .RuleFor(r => r.SensorId, f => f.Random.Guid().ToString())
                .RuleFor(r => r.SensorStatus, f => f.PickRandom<SensorStatus>())
                .RuleFor(r => r.Timestamp, f => f.Date.RecentOffset())
                .Generate();
        }

        private async Task ProduceEventAsync(SensorUpdate sensorUpdate, string operationId, string transactionId)
        {
            EventData message =
                EventDataBuilder.CreateForBody(sensorUpdate)
                                .WithOperationId(operationId)
                                .WithTransactionId(transactionId)
                                .Build();

            // Not all EventHubs functionality in the Azure SDK supports the metadata retrieval of the `CorrelationId` property, so here we should use the properties instead.
            message.Properties["Operation-Id"] = operationId;

            EventHubsConfig eventHubsConfig = _configuration.GetEventHubsConfig();
            await using (var client = new EventHubProducerClient(eventHubsConfig.EventHubsConnectionString, eventHubsConfig.EventHubsName))
            {
                await client.SendAsync(new[] { message });
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_serviceBusMessageEventConsumer != null)
            {
                await _serviceBusMessageEventConsumer.DisposeAsync();
            }
        }
    }
}
