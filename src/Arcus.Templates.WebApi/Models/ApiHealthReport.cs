using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Arcus.Templates.WebApi.Models
{
    /// <summary>
    /// Represents an alternative <see cref="HealthReport"/> model without the exception details so it can be exposed safely with OpenAPI.
    /// </summary>
    /// <remarks>
    ///     This model should not be used within the domain and is just available for the serialization between trust boundaries.
    ///     Use the <see cref="FromHealthReport"/> and <see cref="ToHealthReport"/> to switch between the two.
    /// </remarks>
    public class ApiHealthReport
    {
        /// <summary>
        /// Gets a dictionary containing the results from each health check.
        /// </summary>
        public IDictionary<string, ApiHealthReportEntry> Entries { get; set; } = new Dictionary<string, ApiHealthReportEntry>();

        /// <summary>
        /// Gets a <see cref="HealthStatus"/> representing the aggregate status of all the health checks.
        /// The value of <see cref="Status"/> will be the most severe status reported by a health check.
        /// If no checks were executed, the value is always <see cref="HealthStatus.Healthy"/>.
        /// </summary>
        public HealthStatus Status { get; set; } = HealthStatus.Healthy;

        /// <summary>
        /// Gets the time the health check service took to execute.
        /// </summary>
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// Creates a JSON data-transfer object from the given <paramref name="report"/>.
        /// </summary>
        /// <param name="report">The finalized health report.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="report"/> is <c>null</c>.</exception>
        public static ApiHealthReport FromHealthReport(HealthReport report)
        {
            ArgumentNullException.ThrowIfNull(report);

            IDictionary<string, ApiHealthReportEntry> entries = 
                report.Entries.ToDictionary(
                    item => item.Key, 
                    item => ApiHealthReportEntry.FromHealthReportEntry(item.Value));
            
            return new ApiHealthReport
            {
                Entries = entries,
                Status = report.Status,
                TotalDuration = report.TotalDuration
            };
        }

        /// <summary>
        /// Creates a JSON data-transfer object from the given <paramref name="report"/>.
        /// </summary>
        /// <param name="report">The finalized health report.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="report"/> is <c>null</c>.</exception>
        public static HealthReport ToHealthReport(ApiHealthReport report)
        {
            ArgumentNullException.ThrowIfNull(report);

            IDictionary<string, HealthReportEntry> entries =
                report.Entries.ToDictionary(
                    item => item.Key, 
                    item => ApiHealthReportEntry.ToHealthReportEntry(item.Value));

            return new HealthReport(
                new ReadOnlyDictionary<string, HealthReportEntry>(entries),
                report.TotalDuration);
        }
    }
}