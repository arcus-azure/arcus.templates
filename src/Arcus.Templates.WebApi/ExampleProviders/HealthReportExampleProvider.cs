using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
                ["api"] = new HealthReportEntry(HealthStatus.Healthy, "Api is healthy", TimeSpan.FromMilliseconds(120), null, null)
            };
            return new HealthReport(new ReadOnlyDictionary<string, HealthReportEntry>(new Dictionary<string, HealthReportEntry>()), TimeSpan.FromMilliseconds(201));
        }
    }
}

