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
    public class HealthReportResponseExampleProvider : IExamplesProvider<HealthReportJson>
    {
        /// <summary>
        /// Build the <see cref="HealthReportJson"/> response example
        /// </summary>
        /// <returns>A populated <see cref="HealthReportJson"/> object that acts as the example included in the OpenAPI documentation.</returns>
        public HealthReportJson GetExamples()
        {
            var healthyApiEntry = new HealthReportEntryJson()
            {
                Status = HealthStatus.Healthy,
                Description = "Api is healthy",
                Duration = TimeSpan.FromMilliseconds(33)
            };
            var healthyDatabaseEntry = new HealthReportEntryJson()
            {
                Status = HealthStatus.Healthy,
                Description = "Database is available",
                Duration = TimeSpan.FromMilliseconds(123)
            };

            var entries = new Dictionary<string, HealthReportEntryJson>
            {
                ["api"] = healthyApiEntry,
                ["database"]= healthyDatabaseEntry,
            };

            return new HealthReportJson()
            {
                Entries = new ReadOnlyDictionary<string, HealthReportEntryJson>(entries),
                TotalDuration = TimeSpan.FromMilliseconds(201)
            };
        }
    }
}