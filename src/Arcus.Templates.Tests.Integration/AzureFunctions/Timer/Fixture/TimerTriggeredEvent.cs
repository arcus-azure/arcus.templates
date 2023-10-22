using System;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Timer.Fixture
{
    public class TimerTriggeredEvent
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public string TimerName { get; set; }
        public DateTimeOffset TriggeredDate { get; set; }
    }
}
