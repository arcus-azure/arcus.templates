using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Fixture
{
    /// <summary>
    /// Representation of an endpoint of the API, exposing the available HTTP methods to interact in a friendly manner.
    /// </summary>
    public abstract class EndpointService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointService"/> class.
        /// </summary>
        /// <param name="outputWriter">The logger to trace messages during interaction with the API.</param>
        protected EndpointService(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter));

            Logger = outputWriter;
        }

        /// <summary>
        /// Gets the HTTP client used during the interaction with the API.
        /// </summary>
        protected static HttpClient HttpClient { get; } = new HttpClient();
         
        /// <summary>
        /// Gets the logger that traces messages during the interaction with the API.
        /// </summary>
        protected  ITestOutputHelper Logger { get; }

        /// <summary>
        /// Sends a GET request to the specific <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="endpoint">The target endpoint to which the request must be send.</param>
        /// <param name="alterRequest">The custom function to alter the request before sending.</param>
        protected async Task<HttpResponseMessage> GetAsync(string endpoint, Action<HttpRequestMessage> alterRequest = null)
        {
            Guard.NotNull(endpoint, nameof(endpoint));

            Logger.WriteLine("GET -> {0}", endpoint);
            using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint))
            {
                alterRequest?.Invoke(request);
                HttpResponseMessage response = await HttpClient.SendAsync(request);

                IEnumerable<string> headers = response.Headers.Select(h => $"{h.Key}={String.Join("", h.Value)}");
                Logger.WriteLine("{0} <- {1} {{{2}}}", response.StatusCode, endpoint, String.Join(", ", headers));
                return response;
            }
        }
    }
}
