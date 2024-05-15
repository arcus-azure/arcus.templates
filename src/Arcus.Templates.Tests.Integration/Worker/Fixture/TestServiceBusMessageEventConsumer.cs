using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Templates.Tests.Integration.Fixture;
using Azure.Messaging;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Templates.Tests.Integration.Worker.Fixture
{
    /// <summary>
    /// Represents an event consumer which receives events from an Azure Service Bus.
    /// </summary>
    public class TestServiceBusMessageEventConsumer : IAsyncDisposable
    {
        private readonly ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        private TestServiceBusMessageEventConsumer(ServiceBusEventConsumerHost consumerHost)
        {
            Guard.NotNull(consumerHost, nameof(consumerHost), "Requires an Azure Service Bus consumer host instance to consume messages");
            _serviceBusEventConsumerHost = consumerHost;
        }

        /// <summary>
        /// Starts an new event consumer which receives events from an Azure Service Bus entity.
        /// </summary>
        /// <param name="configuration">The test configuration to retrieve the Azure Service Bus test infrastructure.</param>
        /// <param name="logger">The logger to write diagnostic messages during consuming the messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static async Task<TestServiceBusMessageEventConsumer> StartNewAsync(TestConfig configuration, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the Azure Service Bus test infrastructure");

            logger = logger ?? NullLogger.Instance;

            var topicName = configuration.GetValue<string>("Arcus:Worker:Infra:ServiceBus:TopicName");
            var connectionString = configuration.GetValue<string>("Arcus:Worker:Infra:ServiceBus:ConnectionString");
            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);

            var serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, logger);
            return new TestServiceBusMessageEventConsumer(serviceBusEventConsumerHost);
        }

        /// <summary>
        /// Receives an event produced on the Azure Service Bus.
        /// </summary>
        /// <param name="eventId">The ID to identity the produced event.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventId"/> is blank.</exception>
        public TEventData ConsumeEvent<TEventData>(string eventId)
        {
            string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(eventId, retryCount: 10);
            Assert.NotEmpty(receivedEvent);

            BinaryData data = BinaryData.FromString(receivedEvent);
            var cloudEvent = CloudEvent.Parse(data);
            Assert.NotNull(cloudEvent.Data);

            string json = cloudEvent.Data.ToString();
            return JsonConvert.DeserializeObject<TEventData>(json, new MessageCorrelationInfoJsonConverter());
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _serviceBusEventConsumerHost.StopAsync();
        }
    }
}
