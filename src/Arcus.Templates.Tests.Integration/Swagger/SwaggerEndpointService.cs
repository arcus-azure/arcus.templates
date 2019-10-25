using System;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Flurl;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Swagger
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
            _swaggerUIEndpoint = rootUrl.AppendPathSegment("swagger");
            _swaggerDocsEndpoint =
                _swaggerUIEndpoint.AppendPathSegment("v1")
                                  .AppendPathSegment("swagger.json");
        }

        /// <summary>
        /// Sends a GET request to the Swagger UI help page.
        /// </summary>
        public async Task<HttpResponseMessage> GetSwaggerUI()
        {
            return await GetAsync(_swaggerUIEndpoint);
        }

        /// <summary>
        /// Sends a GET request to the Swagger JSON docs page.
        /// </summary>
        public async Task<HttpResponseMessage> GetSwaggerDocs()
        {
            return await GetAsync(_swaggerDocsEndpoint);
        }
    }
}
