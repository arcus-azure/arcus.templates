using System;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a default implementation of the <see cref="EndpointService"/> to send out minimal HTTP requests starting from the base path of the web application.
    /// </summary>
    public class RootEndpointService : EndpointService
    {
        private readonly Uri _baseUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointService"/> class.
        /// </summary>
        /// <param name="baseUri">The root path of the web API application HTTP routes.</param>
        /// <param name="outputWriter">The logger to trace messages during interaction with the API.</param>
        public RootEndpointService(Uri baseUri, ITestOutputHelper outputWriter) : base(outputWriter)
        {
            Guard.NotNull(baseUri, nameof(baseUri));

            _baseUri = baseUri;
        }

        /// <summary>
        /// Sends a HTTP GET request starting from the base root HTTP path of the web application, and appending the given <paramref name="route"/>.
        /// </summary>
        /// <param name="route">The specific route to which the HTTP GET request should be sent.</param>
        public async Task<HttpResponseMessage> GetAsync(string route)
        {
            Guard.NotNullOrWhitespace(route, nameof(route), "Requires a HTTP route");
            
            string endpoint = _baseUri.OriginalString.AppendPathSegments(route);
            return await base.GetAsync(endpoint);
        }
    }
}
