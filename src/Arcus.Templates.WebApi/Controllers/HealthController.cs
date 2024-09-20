using System;
using System.Threading.Tasks;
using Arcus.Templates.WebApi.Models;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
#if OpenApi
using Arcus.Templates.WebApi.ExampleProviders;
using Swashbuckle.AspNetCore.Filters;
#endif

namespace Arcus.Templates.WebApi.Controllers
{
    /// <summary>
    /// API endpoint to check the health of the application.
    /// </summary>
    [ApiController]
    [Route("api/v1/health")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController"/> class.
        /// </summary>
        /// <param name="healthCheckService">The service to provide the health of the API application.</param>
        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        }

        /// <summary>
        ///     Get Health
        /// </summary>
        /// <remarks>Provides an indication about the health of the API.</remarks>
        /// <response code="200">API is healthy</response>
        /// <response code="503">API is unhealthy or in degraded state</response>
        [HttpGet(Name = "Health_Get")]
        [RequestTracking(500, 599)]
        [ProducesResponseType(typeof(ApiHealthReport), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiHealthReport), StatusCodes.Status503ServiceUnavailable)]
#if OpenApi
#if Correlation
        [SwaggerResponseHeader(200, "X-Transaction-ID", "string", "The header that has the transaction ID is used to correlate multiple operation calls")]
        [SwaggerResponseHeader(200, "X-Operation-ID", "string", "The header that has the operation ID is used to uniquely identify this single call")]
#endif
        [SwaggerResponseExample(200, typeof(HealthReportResponseExampleProvider))]
#endif
        public async Task<IActionResult> Get()
        {
            HealthReport healthReport = await _healthCheckService.CheckHealthAsync();
            ApiHealthReport json = ApiHealthReport.FromHealthReport(healthReport);

            if (healthReport.Status == HealthStatus.Healthy)
            {
                return Ok(json);
            }
            else
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, json);
            }
        }
    }
}
