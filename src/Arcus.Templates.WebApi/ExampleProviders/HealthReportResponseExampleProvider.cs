using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Arcus.Templates.WebApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Filters;

namespace Arcus.Templates.WebApi.ExampleProviders
{
    /// <summary>
    /// Generates an example response object for the health API endpoint that will be included in the OpenAPI documentation.
    /// </summary>
    public class HealthReportResponseExampleProvider : IExamplesProvider<ApiHealthReport>
    {
        /// <summary>
        /// Build the <see cref="ApiHealthReport"/> response example
        /// </summary>
        /// <returns>A populated <see cref="ApiHealthReport"/> object that acts as the example included in the OpenAPI documentation.</returns>
        public ApiHealthReport GetExamples()
        {
            var healthyApiEntry = new ApiHealthReportEntry()
            {
                Status = HealthStatus.Healthy,
                Description = "Api is healthy",
                Duration = TimeSpan.FromMilliseconds(33)
            };
            var healthyDatabaseEntry = new ApiHealthReportEntry()
            {
                Status = HealthStatus.Healthy,
                Description = "Database is available",
                Duration = TimeSpan.FromMilliseconds(123)
            };

            var entries = new Dictionary<string, ApiHealthReportEntry>
            {
                ["api"] = healthyApiEntry,
                ["database"]= healthyDatabaseEntry,
            };

            return new ApiHealthReport()
            {
                Entries = new ReadOnlyDictionary<string, ApiHealthReportEntry>(entries),
                TotalDuration = TimeSpan.FromMilliseconds(201)
            };
        }
    }
}