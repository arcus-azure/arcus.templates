using System;
using Arcus.Messaging.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    public class SensorUpdateEventData
    {
        public string SensorId { get; set; }
        public SensorStatus SensorStatus { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public MessageCorrelationInfo CorrelationInfo { get; set; }
    }
}
