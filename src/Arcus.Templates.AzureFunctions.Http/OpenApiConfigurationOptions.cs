using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Arcus.Templates.AzureFunctions.Http
{
    /// <summary>
    /// Represents the custom OpenAPI configuration options of the Azure Function, specifying global information of the OpenAPI documentation.
    /// </summary>
    public class OpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
    {
        /// <inheritdoc />
        public override OpenApiInfo Info { get; set; } = new OpenApiInfo
        {
            Version = GetOpenApiDocVersion(),
            Title = Environment.GetEnvironmentVariable("OpenApi__DocTitle") ?? "Arcus.Templates.AzureFunctions.Http"
        };

        /// <inheritdoc />
        public override OpenApiVersionType OpenApiVersion { get; set; } = GetOrLoadOpenApiVersion();

        private static OpenApiVersionType GetOrLoadOpenApiVersion()
        {
            string environmentOpenApiVersion = Environment.GetEnvironmentVariable("OpenApi__Version");
            if (Enum.TryParse(environmentOpenApiVersion, true, out OpenApiVersionType result))
            {
                return result;
            }

            return OpenApiVersionType.V3;
        }
    }
}
