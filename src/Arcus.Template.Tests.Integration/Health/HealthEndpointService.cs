using System;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Template.Tests.Integration.Fixture;
using Flurl;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Template.Tests.Integration.Health
{
    /// <summary>
    /// Service to collect operations on the health functionality of the API.
    /// </summary>
    public class HealthEndpointService
    {
        private readonly string _healthEndpoint;
        private readonly ITestOutputHelper _outputWriter;
        
        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndpointService"/> class.
        /// </summary>
        /// <param name="configuration">The configuration used to call the health endpoints of the API.</param>
        /// <param name="outputWriter">The test logger to document run operations in the health functionality.</param>
        public HealthEndpointService(TestConfig configuration, ITestOutputHelper outputWriter)
            : this(configuration?.GetDockerBaseUrl(), outputWriter)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndpointService"/> class.
        /// </summary>
        /// <param name="baseUrl">The base URL to where the health endpoint will be located.</param>
        /// <param name="outputWriter">The test logger to document run operations in the health functionality.</param>
        public HealthEndpointService(Uri baseUrl, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter));

            string healthEndpoint = baseUrl?.OriginalString?.AppendPathSegments("health");
            Guard.NotNullOrWhitespace(healthEndpoint, nameof(healthEndpoint), "Provided test configuration doesn't contain a base API url to construct a health endpoint from");

            _healthEndpoint = healthEndpoint;
            _outputWriter = outputWriter;
        }

        /// <summary>
        /// Sends a GET request to the health endpoint of the API web application.
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync()
        {
            return await GetAsync(request => { });
        }

        /// <summary>
        /// Sends a GET request to the health endpoint of the API web application.
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync(Action<HttpRequestMessage> alterRequest)
        {
            _outputWriter.WriteLine("GET -> {0}", _healthEndpoint);
            using (var request = new HttpRequestMessage(HttpMethod.Get, _healthEndpoint))
            {
                alterRequest(request);
                HttpResponseMessage response = await HttpClient.SendAsync(request);

                _outputWriter.WriteLine("{0} <- {1}", response.StatusCode, _healthEndpoint);
                return response;
            }
        }
    }
}
