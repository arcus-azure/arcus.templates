using Newtonsoft.Json;

namespace Arcus.Templates.EventHubs.Model
{
    public class SensorReading
    {
        [JsonProperty]
        public string? SensorId { get; set; }

        [JsonProperty]
        public string? SensorValue { get; set; }
    }
}