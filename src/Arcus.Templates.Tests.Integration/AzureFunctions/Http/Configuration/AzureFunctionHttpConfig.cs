using System;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Configuration
{
    /// <summary>
    /// Represents the application configuration set to interact with an HTTP trigger Azure Function.
    /// </summary>
    public class AzureFunctionHttpConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref=AzureFunctionHttpConfig"/> class.
        /// </summary>
        /// <param name="httpPort">The HTTP port where the Azure Functions project will be running.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="httpPort"/> is less then zero.</exception>
        public AzureFunctionHttpConfig(int httpPort)
        {
            Guard.NotLessThan(httpPort, 0, nameof(httpPort), "Requires a HTTP port that's greater than zero to locate the endpoint where the Azure Functions project is running");
            HttpPort = httpPort;
        }
        
        /// <summary>
        /// Gets the HTTP port where the Azure Function project will be running.
        /// </summary>
        public int HttpPort { get; }
    }
}
