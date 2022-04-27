using System;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Worker.Configuration
{
    /// <summary>
    /// Represents the configuration values for using an Azure Event Grid resource during the integration tests.
    /// </summary>
    public class EventGridConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridConfig" /> class.
        /// </summary>
        /// <param name="topicUri">The Azure Event Grid topic URI where the resource is located.</param>
        /// <param name="authenticationKey">The authentication key to interact with the Azure Event Grid resource.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicUri"/> or <paramref name="authenticationKey"/> is blank.</exception>
        public EventGridConfig(string topicUri, string authenticationKey)
        {
            Guard.NotNullOrWhitespace(topicUri, nameof(topicUri), "Requires a non-blank Azure Event Grid topic URI to locate the resource");
            Guard.NotNullOrWhitespace(authenticationKey, nameof(authenticationKey), "Requires a non-blank authentication key to interact with the Azure Event Grid resource");

            TopicUri = topicUri;
            AuthenticationKey = authenticationKey;
        }

        /// <summary>
        /// Gets the Azure Event Grid topic URI where the resource is located.
        /// </summary>
        public string TopicUri { get; }

        /// <summary>
        /// Gets the authentication key to interact with the Azure Event Grid resource.
        /// </summary>
        public string AuthenticationKey { get; }
    }
}
