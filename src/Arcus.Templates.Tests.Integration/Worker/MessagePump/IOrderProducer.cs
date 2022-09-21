using System;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Worker.Fixture;

namespace Arcus.Templates.Tests.Integration.Worker.MessagePump
{
    /// <summary>
    /// Represents a message producer of orders towards an Azure resource.
    /// </summary>
    public interface IOrderProducer
    {
        /// <summary>
        /// Sends the <paramref name="order"/> to the configured Azure resource.
        /// </summary>
        /// <param name="order">The message to send.</param>
        /// <param name="operationId">The ID to identify a single operation in a correlation scenario.</param>
        /// <param name="transactionId">The ID to identify a whole transaction across interactions in a correlation scenario.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="order"/> is <c>null</c>.</exception>
        Task ProduceAsync(Order order, string operationId, string transactionId);
    }
}
