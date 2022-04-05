using Newtonsoft.Json;

namespace Arcus.Templates.AzureFunctions.ServiceBus.Queue.Model
{
    public class Order
    {
        [JsonProperty]
        public string Id { get; set; }

        [JsonProperty]
        public int Amount { get; set; }

        [JsonProperty]
        public string ArticleNumber { get; set; }
    }
}