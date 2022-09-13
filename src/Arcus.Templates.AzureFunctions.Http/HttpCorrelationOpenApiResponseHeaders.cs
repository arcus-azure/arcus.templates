using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Models;

namespace Arcus.Templates.AzureFunctions.Http
{
    /// <summary>
    /// Represents a custom OpenAPI response header definition for the HTTP correlation.
    /// </summary>
    public class HttpCorrelationOpenApiResponseHeaders : IOpenApiCustomResponseHeader
    {
        /// <summary>
        /// Gets or sets the collection of the <see cref="T:Microsoft.OpenApi.Models.OpenApiHeader" /> instances.
        /// </summary>
        public Dictionary<string, OpenApiHeader> Headers { get; set; } = new Dictionary<string, OpenApiHeader>
        {
            {
                "X-Transaction-Id",
                new OpenApiHeader
                {
                    Required = true,
                    Description = "Transaction ID is used to correlate multiple operation calls. A new transaction ID will be generated if no `X-Transaction-Id` header was present in the request"
                }
            },
            {
                "X-Operation-Id",
                new OpenApiHeader
                {
                    Required = true,
                    Description = "Operation ID is used to uniquely identify a single operation call"
                }
            },
            {
                "RequestId",
                new OpenApiHeader
                {
                    Required = true,
                    Description = "Request ID is used to identify the upstream service that calls this endpoint"
                }
            }
        };
    }
}
