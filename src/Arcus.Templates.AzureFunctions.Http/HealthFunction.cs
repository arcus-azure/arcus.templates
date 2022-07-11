using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
#if OpenApi
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums; 
#endif
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
#if OpenApi
using Microsoft.OpenApi.Models; 
#endif

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
#if OpenApi
        [OpenApiOperation("Health_Get", tags: new[] { "health" }, Summary = "Gets the health report", Description = "Gets the current health report of the running Azure Function", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter("X-Transaction-Id", In = ParameterLocation.Header, Type = typeof(string), Required = false, Summary = "The correlation transaction ID", Description = "The correlation transaction ID is used to correlate multiple operation calls")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string), Summary = "The health report summary", Description = "The health report summary of all the run health checks", CustomHeaderType = typeof(HttpCorrelationOpenApiResponseHeaders))]
        [OpenApiResponseWithBody(HttpStatusCode.UnsupportedMediaType, "text/plain", typeof(string), Summary = "The faulted response for non-JSON requests", Description = "The faulted response (415) when the request doesn't accept JSON")]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string), Summary = "The faulted response for invalid correlation requests", Description = "The faulted response (400) when the request doesn't correlate correctly")]
        [OpenApiResponseWithBody(HttpStatusCode.InternalServerError, "text/plain", typeof(string), Summary = "The faulted response for general failures", Description = "The faulted response (500) for any general and unexpected server-related failure")]
        [OpenApiResponseWithBody(HttpStatusCode.ServiceUnavailable, "application/json", typeof(string), Summary = "The faulted response for unhealthy reports", Description = "The faulted response (503) when the health checks results in an unhealthy report", CustomHeaderType = typeof(HttpCorrelationOpenApiResponseHeaders))]
#endif
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

                return Json(healthReport, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, exception.Message);
                return InternalServerError("Could not process the current request due to an unexpected exception");
            }
        } 
    }
}
