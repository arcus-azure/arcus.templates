using Newtonsoft.Json;

namespace Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture
{
    public class Customer
    {
        [JsonProperty]
        public string FirstName { get; private set; }

        [JsonProperty]
        public string LastName { get; private set; }
    }
}