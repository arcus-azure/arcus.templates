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
                    Description = "Transaction ID is used to correlate multiple operation calls. A new transaction ID will be generated if request did not specified one."
                }
            },
            {
                "RequestId",
                new OpenApiHeader
                {
                    Required = true,
                    Description = "Operation ID is used to identify a single operation call."
                }
            }
        };
    }
}
