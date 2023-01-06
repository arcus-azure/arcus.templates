using System;

namespace Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture
{
    public class SensorUpdate
    {
        public string SensorId { get; set; }
        public SensorStatus SensorStatus { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
