using System;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Configuration
{
    /// <summary>
    /// Represents the application configuration set to interact with an HTTP trigger Azure Function.
    /// </summary>
    public class AzureFunctionHttpConfig
    {
        private readonly int _isolatedHttpPort, _inProcessHttpPort;

        /// <summary>
        /// Initializes a new instance of the <see cref=AzureFunctionHttpConfig"/> class.
        /// </summary>
        /// <param name="isolatedHttpPort">The HTTP port where the isolated Azure Functions project will be running.</param>
        /// <param name="inProcessHttpPort">The HTTP port where the in-process Azure Functions project will be running.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="isolatedHttpPort"/> is less then zero.</exception>
        public AzureFunctionHttpConfig(int isolatedHttpPort, int inProcessHttpPort)
        {
            Guard.NotLessThan(isolatedHttpPort, 0, nameof(isolatedHttpPort), "Requires a HTTP port that's greater than zero to locate the endpoint where the isolated Azure Functions project is running");
            Guard.NotLessThan(inProcessHttpPort, 0, nameof(inProcessHttpPort), "Requires a HTTP port that's greater than zero to locate the endpoint where the in-process Azure Functions project is running");
            
            _isolatedHttpPort = isolatedHttpPort;
            _inProcessHttpPort = inProcessHttpPort;
        }

        /// <summary>
        /// Gets the HTTP port associated with the running Azure Functions Docker project.
        /// </summary>
        /// <param name="workerType">The functions worker type of the running Azure Functions Docker project.</param>
        public int GetHttpPort(FunctionsWorker workerType)
        {
            switch (workerType)
            {
                case FunctionsWorker.InProcess: return _inProcessHttpPort;
                case FunctionsWorker.Isolated: return _isolatedHttpPort;
                default:
                    throw new ArgumentOutOfRangeException(nameof(workerType), workerType, "Unknown Azure Functions worker type");
            }
        }
    }
}
