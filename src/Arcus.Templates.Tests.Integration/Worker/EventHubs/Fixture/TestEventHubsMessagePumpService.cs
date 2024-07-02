using System;
using System.IO;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Testing;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Bogus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using TestConfig = Arcus.Templates.Tests.Integration.Fixture.TestConfig;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    public class TestEventHubsMessagePumpService : IMessagingService
    {
        private readonly TestConfig _configuration;
        private readonly DirectoryInfo _projectDirectory;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestEventHubsMessagePumpService" /> class.
        /// </summary>
        public TestEventHubsMessagePumpService(
            TestConfig configuration,
            DirectoryInfo projectDirectory,
            ITestOutputHelper outputWriter)
        {
            _configuration = configuration;
            _projectDirectory = projectDirectory;
            _logger = new XunitTestLogger(outputWriter);
        }

        public async Task SimulateMessageProcessingAsync()
        {
            var traceParent = TraceParent.Generate();
            SensorUpdate update = GenerateSensorReading();
            await ProduceEventAsync(update, traceParent);

            SensorUpdateEventData sensorReadEventData = await ConsumeEventAsync(traceParent);
            Assert.NotNull(sensorReadEventData);
            Assert.NotNull(sensorReadEventData.CorrelationInfo);
            Assert.Equal(update.SensorId, sensorReadEventData.SensorId);
            Assert.Equal(update.SensorStatus, sensorReadEventData.SensorStatus);
            Assert.Equal(update.Timestamp, sensorReadEventData.Timestamp);
            Assert.Equal(traceParent.TransactionId, sensorReadEventData.CorrelationInfo.TransactionId);
            Assert.Equal(traceParent.OperationParentId, sensorReadEventData.CorrelationInfo.OperationParentId);
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

        private async Task ProduceEventAsync(SensorUpdate sensorUpdate, TraceParent traceParent)
        {
            _logger.LogTrace("Produces an event with transaction ID: {TransactionId}", traceParent.TransactionId);

            var message = new EventData(BinaryData.FromObjectAsJson(sensorUpdate))
            {
                Properties = { ["Diagnostic-Id"] = traceParent.DiagnosticId }
            };

            EventHubsConfig eventHubsConfig = _configuration.GetEventHubsConfig();
            await using var client = new EventHubProducerClient(eventHubsConfig.EventHubsConnectionString, eventHubsConfig.EventHubsName);
            await client.SendAsync(new[] { message });
        }

        private async Task<SensorUpdateEventData> ConsumeEventAsync(TraceParent traceParent)
        {
            _logger.LogTrace("Consumes an event with transaction ID: {TransactionId}", traceParent.TransactionId);

            FileInfo[] foundFiles =
                await Poll.Target(() => Task.FromResult(_projectDirectory.GetFiles(traceParent.TransactionId + ".json", SearchOption.AllDirectories)))
                          .Until(files => files.Length > 0)
                          .Every(TimeSpan.FromMilliseconds(200))
                          .Timeout(TimeSpan.FromMinutes(5))
                          .FailWith("Failed to retrieve the necessary produced message from the temporary project created from the worker project template, " +
                                    "please check whether the injected message handler was correct and if the created project correctly receives the message");

            FileInfo found = Assert.Single(foundFiles);
            string json = await File.ReadAllTextAsync(found.FullName);
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new MessageCorrelationInfoJsonConverter());

            return JsonConvert.DeserializeObject<SensorUpdateEventData>(json, settings);
        }
    }
}
