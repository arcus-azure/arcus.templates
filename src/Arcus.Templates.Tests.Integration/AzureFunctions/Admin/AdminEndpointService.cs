using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.WebApi.Fixture;
using Flurl;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Admin
{
    /// <summary>
    /// Represents a service that acts as a gateway to the running Azure Functions project so the metric reporting function can be manually triggered.
    /// </summary>
    public class AdminEndpointService : EndpointService
    {
        private readonly int _httpPort;
        private readonly string _functionName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminEndpointService"/> class.
        /// </summary>
        public AdminEndpointService(int httpPort, string functionName, ITestOutputHelper outputWriter) 
            : base(outputWriter)
        {
            _httpPort = httpPort;
            _functionName = functionName;
            Endpoint = new Uri($"http://localhost:{_httpPort}/admin/functions/{_functionName}");
        }

        /// <summary>
        /// Gets the admin HTTP endpoint to interact with the Azure Function directly.
        /// </summary>
        public Uri Endpoint { get; }

        /// <summary>
        /// Triggers the running Azure Functions project.
        /// </summary>
        /// <exception cref="HttpRequestException">Thrown when the Azure Functions endpoint cannot be contacted.</exception>
        public async Task TriggerFunctionAsync()
        {
            try
            {
                Logger.WriteLine("POST -> {0}", Endpoint);
                using (var content = new StringContent("{}", Encoding.UTF8, "application/json"))
                using (HttpResponseMessage response = await HttpClient.PostAsync(Endpoint, content))
                {
                    Logger.WriteLine("{0} <- {1}", response.StatusCode, Endpoint);
                }
            }
            catch (Exception exception)
            {
                Logger.WriteLine("Failed to contact the running Azure Functions project: {0}", exception.Message);
                throw new HttpRequestException($"Failed to contact the running Azure Functions project: {exception.Message}", exception);
            }
        }
    }
}
