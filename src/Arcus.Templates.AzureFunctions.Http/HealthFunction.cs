using System;
using System.Net;
using System.Threading.Tasks;
using Arcus.Templates.AzureFunctions.Http.Model;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http; 
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
    public class HealthFunction
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly JsonObjectSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthFunction" /> class.
        /// </summary>
        /// <param name="healthCheckService">The service to check the current health of the running Azure Function.</param>
        /// <param name="jsonSerializer">The configured serializer in the application services that controls how JSON responses are serialized.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="healthCheckService"/> or the <paramref name="jsonSerializer"/> is <c>null</c>.</exception>
        public HealthFunction(HealthCheckService healthCheckService, JsonObjectSerializer jsonSerializer)
        {
            ArgumentNullException.ThrowIfNull(healthCheckService);
            ArgumentNullException.ThrowIfNull(jsonSerializer);
            
            _healthCheckService = healthCheckService;
            _jsonSerializer = jsonSerializer;
        }

        [Function("health")]
#if OpenApi
        [OpenApiOperation("Health_Get", tags: new[] { "health" }, Summary = "Gets the health report", Description = "Gets the current health report of the running Azure Function", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter("traceparent", In = ParameterLocation.Header, Type = typeof(string), Required = false, Summary = "The correlation header", Description = "The correlation header is used to correlate multiple operation calls")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string), Summary = "The health report summary", Description = "The health report summary of all the run health checks", CustomHeaderType = typeof(HttpCorrelationOpenApiResponseHeaders))]
        [OpenApiResponseWithBody(HttpStatusCode.UnsupportedMediaType, "text/plain", typeof(string), Summary = "The faulted response for non-JSON requests", Description = "The faulted response (415) when the request doesn't accept JSON")]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string), Summary = "The faulted response for invalid correlation requests", Description = "The faulted response (400) when the request doesn't correlate correctly")]
        [OpenApiResponseWithBody(HttpStatusCode.InternalServerError, "text/plain", typeof(string), Summary = "The faulted response for general failures", Description = "The faulted response (500) for any general and unexpected server-related failure")]
        [OpenApiResponseWithBody(HttpStatusCode.ServiceUnavailable, "application/json", typeof(string), Summary = "The faulted response for unhealthy reports", Description = "The faulted response (503) when the health checks results in an unhealthy report", CustomHeaderType = typeof(HttpCorrelationOpenApiResponseHeaders))]
#endif  
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/health")] HttpRequestData request,
            FunctionContext context)
        {
            ILogger logger = context.GetLogger<HealthFunction>();
            logger.LogInformation("C# HTTP trigger 'health' function processed a request");
            
            HealthReport healthReport = await _healthCheckService.CheckHealthAsync();
            ApiHealthReport apiHealthReport = ApiHealthReport.FromHealthReport(healthReport);
            
            HttpResponseData healthResponse = request.CreateResponse();
            await healthResponse.WriteAsJsonAsync(apiHealthReport, _jsonSerializer);
            healthResponse.StatusCode = DetermineStatusCode(healthReport);
            
            return healthResponse;
        }
        
        private static HttpStatusCode DetermineStatusCode(HealthReport healthReport)
        {
            if (healthReport?.Status == HealthStatus.Healthy)
            {
               return HttpStatusCode.OK;
            }

            return HttpStatusCode.ServiceUnavailable;
        }
    }
}
