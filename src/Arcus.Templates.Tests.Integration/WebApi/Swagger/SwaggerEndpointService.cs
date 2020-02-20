using System;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.WebApi.Fixture;
using Flurl;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Swagger
{
    /// <summary>
    /// Service to collect operations on the Swagger information of the API.
    /// </summary>
    public class SwaggerEndpointService : EndpointService
    {
        private readonly string _swaggerUIEndpoint, _swaggerDocsEndpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerEndpointService"/> class.
        /// </summary>
        /// <param name="baseUrl">The root URL where the project is hosted.</param>
        /// <param name="outputWriter">The logger to trace HTTP calls to the swagger endpoint.</param>
        public SwaggerEndpointService(Uri baseUrl, ITestOutputHelper outputWriter) : base(outputWriter)
        {
            Guard.NotNull(baseUrl, nameof(baseUrl));

            Url rootUrl = baseUrl.OriginalString.ResetToRoot();
            _swaggerUIEndpoint = rootUrl.AppendPathSegments("api", "docs");
            _swaggerDocsEndpoint = rootUrl.AppendPathSegments("api", "v1", "docs.json");
        }

        /// <summary>
        /// Sends a GET request to the Swagger UI help page.
        /// </summary>
        public async Task<HttpResponseMessage> GetSwaggerUIAsync()
        {
            return await GetAsync(_swaggerUIEndpoint);
        }

        /// <summary>
        /// Sends a GET request to the Swagger JSON docs page.
        /// </summary>
        public async Task<HttpResponseMessage> GetSwaggerDocsAsync()
        {
            return await GetAsync(_swaggerDocsEndpoint);
        }
    }
}
