using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.WebApi.Fixture;
using Flurl;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Health
{
    /// <summary>
    /// Service to collect operations on the health functionality of the API.
    /// </summary>
    public class HealthEndpointService : EndpointService
    {
        private readonly string _healthEndpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndpointService"/> class.
        /// </summary>
        /// <param name="baseUrl">The base URL to where the health endpoint will be located.</param>
        /// <param name="outputWriter">The test logger to document run operations in the health functionality.</param>
        public HealthEndpointService(Uri baseUrl, ITestOutputHelper outputWriter) : base(outputWriter)
        {
            string healthEndpoint = baseUrl?.OriginalString?.AppendPathSegments("health");
            Guard.NotNullOrWhitespace(healthEndpoint, nameof(healthEndpoint), "Provided test configuration doesn't contain a base API url to construct a health endpoint from");

            _healthEndpoint = healthEndpoint;
        }

        /// <summary>
        /// Sends a GET request to the health endpoint of the API web application.
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync()
        {
            return await GetAsync(_healthEndpoint, request => request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")));
        }

        /// <summary>
        /// Sends a GET request to the health endpoint of the API web application.
        /// </summary>
        /// <param name="alterRequest">The custom function to alter the request before sending.</param>
        public async Task<HttpResponseMessage> GetAsync(Action<HttpRequestMessage> alterRequest)
        {
            return await GetAsync(_healthEndpoint, alterRequest);
        }
    }
}
