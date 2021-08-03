using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.AzureFunctions.Http
{
    /// <summary>
    /// Represents the health check Azure Function to verify if the running Azure Function is healthy.
    /// </summary>
    public class HealthFunction : HttpBasedAzureFunction
    {
        private readonly HealthCheckService _healthCheckService;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthFunction" /> class.
        /// </summary>
        /// <param name="healthCheckService">The service to check the current health of the running Azure Function.</param>
        /// <param name="httpCorrelation">The instance to handle the HTTP request correlation.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages while handling the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="httpCorrelation"/> is <c>null</c>.</exception>
        public HealthFunction(
            HealthCheckService healthCheckService, 
            HttpCorrelation httpCorrelation, 
            ILogger<HealthFunction> logger) 
            : base(httpCorrelation, logger)
        {
            Guard.NotNull(healthCheckService, nameof(healthCheckService), "Requires a health check service to check the current health of the running Azure Function");
            _healthCheckService = healthCheckService;
        }

        [FunctionName("health")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/health")] HttpRequest request,
            CancellationToken cancellation)
        {
            try
            {
                Logger.LogInformation("C# HTTP trigger 'health' function processed a request");
                if (AcceptsJson(request) == false)
                {
                    string accept = String.Join(", ", GetAcceptMediaTypes(request));
                    Logger.LogError("Could not process current request because the response could not accept JSON (Accept: {Accept})", accept);
                    
                    return UnsupportedMediaType("Could not process current request because the response could not accept JSON");
                }

                if (TryHttpCorrelate(out string errorMessage) == false)
                {
                    return BadRequest(errorMessage);
                }

                HealthReport healthReport = await _healthCheckService.CheckHealthAsync(cancellation);
                if (healthReport?.Status == HealthStatus.Healthy)
                {
                    return Json(healthReport);
                }

                return new ObjectResult(healthReport)
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, exception.Message);
                return InternalServerError("Could not process the current request due to an unexpected exception");
            }
        } 
    }
}
