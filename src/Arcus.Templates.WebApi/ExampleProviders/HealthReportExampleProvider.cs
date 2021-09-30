using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Filters;

namespace Arcus.Templates.WebApi.ExampleProviders
{
    public class HealthReportExampleProvider : IExamplesProvider<HealthReport>
    {
        public HealthReport GetExamples()
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                ["api"] = new HealthReportEntry(status: HealthStatus.Healthy, description: "Api is healthy", duration: TimeSpan.FromMilliseconds(120), null, null)
            };

            var healthReportEntries = new ReadOnlyDictionary<string, HealthReportEntry>(entries);

            return new HealthReport(entries: healthReportEntries, totalDuration: TimeSpan.FromMilliseconds(201));
        }
    }
}