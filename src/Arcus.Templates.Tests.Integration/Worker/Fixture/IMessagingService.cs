using System;
using System.Threading.Tasks;

namespace Arcus.Templates.Tests.Integration.Worker.Fixture
{

    public interface IMessagingService
    {
        /// <summary>
        /// Simulate the message processing of the message pump using the Service Bus.
        /// </summary>
        Task SimulateMessageProcessingAsync();
    }
}
