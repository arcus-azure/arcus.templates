using System;
using Arcus.Observability.Correlation;
using System.Diagnostics;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.AzureFunctions.Timer
{
    public class TimerFunction
    {
        private readonly ISecretProvider _secretProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerFunction" /> class.
        /// </summary>
        /// <param name="secretProvider">The instance that provides secrets to the Timer trigger.</param>
        /// <param name="loggerFactory">The factory to create logger instance to write diagnostic information during scheduled runs.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> or <paramref name="loggerFactory"/> is <c>null</c>.</exception>
        public TimerFunction(ISecretProvider secretProvider, ILoggerFactory loggerFactory)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            
            _secretProvider = secretProvider;
            _logger = loggerFactory.CreateLogger<TimerFunction>();
        }

        [Function("timer")]
        public void Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] MyInfo timer)
        {
            _logger.LogInformation("C# Timer trigger function executed at: {Time}", DateTimeOffset.UtcNow);
            _logger.LogInformation("Next timer schedule at: {Next}", timer.ScheduleStatus.Next);
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Represents an <see cref="ICorrelationInfoAccessor"/> implementation that solely retrieves the correlation information from the <see cref="Activity.Current"/>.
    /// Mostly used for places where the Application Insights is baked in and there is no way to hook in custom Arcus functionality.
    /// </summary>
    internal class ActivityCorrelationInfoAccessor : ICorrelationInfoAccessor
    {
        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo GetCorrelationInfo()
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return null;
            }

            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                string operationParentId = DetermineW3CParentId(activity);
                return new CorrelationInfo(
                    activity.SpanId.ToHexString(),
                    activity.TraceId.ToHexString(),
                    operationParentId);
            }

            return new CorrelationInfo(
                activity.Id,
                activity.RootId,
                activity.ParentId);
        }

        private static string DetermineW3CParentId(Activity activity)
        {
            if (activity.ParentSpanId != default)
            {
                return activity.ParentSpanId.ToHexString();
            }
            
            if (!string.IsNullOrEmpty(activity.ParentId))
            {
                // W3C activity with non-W3C parent must keep parentId
                return activity.ParentId;
            }

            return null;
        }

        /// <summary>
        /// Sets the current correlation information for this context.
        /// </summary>
        /// <param name="correlationInfo">The correlation model to set.</param>
        public void SetCorrelationInfo(CorrelationInfo correlationInfo)
        {
            throw new InvalidOperationException(
                "Cannot set new correlation information in Azure Functions in-process model");
        }
    }
}
