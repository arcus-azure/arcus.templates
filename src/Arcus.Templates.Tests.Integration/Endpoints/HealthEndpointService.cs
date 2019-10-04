﻿using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Flurl;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Endpoints
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
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            string healthEndpoint = configuration.GetBaseUrl()?.AppendPathSegments("health");
            Guard.NotNullOrWhitespace(healthEndpoint, nameof(healthEndpoint), "Provided test configuration doesn't contain a base API url to construct a health endpoint from");

            _healthEndpoint = healthEndpoint;
            _outputWriter = outputWriter;
        }

        /// <summary>
        /// Sends a GET request to the health endpoint of the API web application.
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync()
        {
            _outputWriter.WriteLine("GET {0} ->", _healthEndpoint);
            var response = await HttpClient.GetAsync(_healthEndpoint);
            _outputWriter.WriteLine("<- {0}", response.StatusCode);

            return response;
        }
    }
}
