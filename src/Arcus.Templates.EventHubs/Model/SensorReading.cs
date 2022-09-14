using Newtonsoft.Json;

namespace Arcus.Templates.AzureFunctions.ServiceBus.Queue.Model
{
    public class SensorReading
    {
        [JsonProperty]
        public string SensorId { get; set; }
    }
}