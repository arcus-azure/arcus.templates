using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.WebApi.Fixture;
using Flurl;
using GuardNet;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Health
{
    /// <summary>
    /// Service to collect operations on the health functionality of the application.
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
        /// Sends a GET request to the health endpoint of the application.
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync()
        {
            return await GetAsync(_healthEndpoint, request => request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")));
        }

        /// <summary>
        /// Sends a GET request to the health endpoint of the application.
        /// </summary>
        /// <param name="alterRequest">The custom function to alter the request before sending.</param>
        public async Task<HttpResponseMessage> GetAsync(Action<HttpRequestMessage> alterRequest)
        {
            return await GetAsync(_healthEndpoint, alterRequest);
        }

        /// <summary>
        /// Sends a GET request to the health endpoint of the application, parsing the result as a valid <see cref="HealthStatus"/> value.
        /// </summary>
        public async Task<HealthStatus> GetHealthAsync()
        {
            using (HttpResponseMessage response = await GetAsync())
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string json = await response.Content.ReadAsStringAsync();
                var jobject = JObject.Parse(json);

                JToken statusToken = jobject["status"];
                Assert.NotNull(statusToken);
                Assert.True(Enum.TryParse(statusToken.Value<string>(), out HealthStatus status));

                return status;
            }
        }
    }
}
