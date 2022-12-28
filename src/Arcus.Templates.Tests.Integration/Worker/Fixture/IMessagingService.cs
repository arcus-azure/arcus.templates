using System;
using System.Threading.Tasks;

namespace Arcus.Templates.Tests.Integration.Worker.Fixture
{

    public interface IMessagingService : IAsyncDisposable
    {
        /// <summary>
        /// Starts a new instance of the <see cref="IMessagingService"/> type to simulate messages.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Simulate the message processing of the message pump using the Service Bus.
        /// </summary>
        Task SimulateMessageProcessingAsync();
    }
}
