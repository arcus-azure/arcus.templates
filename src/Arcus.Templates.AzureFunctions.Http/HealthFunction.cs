using System;
using System.Threading.Tasks;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.AzureFunctions.Http
{
    /// <summary>
    /// Represents the endpoint that communicates the current health of the running Azure Function.
    /// </summary>
    public class HealthFunction : AzureFunction
    {
        private readonly HealthCheckService _healthService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthFunction"/> class.
        /// </summary>
        /// <param name="healthService">The service that provides the health status of the current running function.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="healthService"/> is <c>null</c>.</exception>
        public HealthFunction(HealthCheckService healthService, HttpCorrelation httpCorrelation, ILogger<HealthFunction> logger) : base(httpCorrelation, logger)
        {
            Guard.NotNull(healthService, nameof(healthService), "Requires a service that provides the current health of the function");
            _healthService = healthService;
        }

        /// <summary>
        /// Gets the current health report of the running Azure Function.
        /// </summary>
        /// <param name="request">The incoming HTTP request.</param>
        /// <param name="logger">The logger to write diagnostic messages.</param>
        [FunctionName("health")]
        [ProducesResponseType(typeof(HealthReport), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthReport), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest request,
            ILogger logger)
        {
            try
            {
                if (TryHttpCorrelate(out string errorMessage) == false)
                {
                    return BadRequest(errorMessage);
                }

                HealthReport healthReport = await _healthService.CheckHealthAsync();
                if (healthReport?.Status == HealthStatus.Healthy)
                {
                    return Ok(healthReport);
                }
                else
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, healthReport);
                }
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, exception.Message);
                return InternalServerError(exception);
            }
        }
    }
}
