using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Filters;

namespace Arcus.Templates.WebApi.ExampleProviders
{
    /// <summary>
    /// Generates an example response object for the health API endpoint that will be included in the OpenAPI documentation.
    /// </summary>
    public class HealthReportResponseExampleProvider : IExamplesProvider<HealthReport>
    {
        /// <summary>
        /// Build the HealthReport response example
        /// </summary>
        /// <returns>A populated HealthReport object that acts as the example included in the OpenAPI documentation.</returns>
        public HealthReport GetExamples()
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                ["api"] = new HealthReportEntry(status: HealthStatus.Healthy, description: "Api is healthy", duration: TimeSpan.FromMilliseconds(33), null, null),
                ["database"]= new HealthReportEntry(status: HealthStatus.Healthy, description: "Database is available", duration: TimeSpan.FromMilliseconds(123), null, null),
            };

            var healthReportEntries = new ReadOnlyDictionary<string, HealthReportEntry>(entries);

            return new HealthReport(entries: healthReportEntries, totalDuration: TimeSpan.FromMilliseconds(201));
        }
    }
}