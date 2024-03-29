﻿using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Templates.AzureFunctions.Http.Model;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using Azure.Core.Serialization;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
#if Isolated
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http; 
#endif
#if InProcess
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http; 
#endif
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
#if InProcess
    public class HealthFunction : HttpBasedAzureFunction 
#endif
#if Isolated
    public class HealthFunction
#endif
    {
        private readonly HealthCheckService _healthCheckService;
#if InProcess
        private readonly AzureFunctionsInProcessHttpCorrelation _httpCorrelation;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthFunction" /> class.
        /// </summary>
        /// <param name="healthCheckService">The service to check the current health of the running Azure Function.</param>
        /// <param name="httpCorrelation">The instance to handle the HTTP request correlation.</param>
        /// <param name="correlationInfoAccessor">The instance to access the HTTP correlation throughout the application.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages while handling the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="httpCorrelation"/> is <c>null</c>.</exception>
        public HealthFunction(
            HealthCheckService healthCheckService, 
            AzureFunctionsInProcessHttpCorrelation httpCorrelation, 
            IHttpCorrelationInfoAccessor correlationInfoAccessor,
            ILogger<HealthFunction> logger) 
            : base(httpCorrelation, correlationInfoAccessor, logger) 
        {
            Guard.NotNull(healthCheckService, nameof(healthCheckService), "Requires a health check service to check the current health of the running Azure Function");
            _healthCheckService = healthCheckService;
            _httpCorrelation = httpCorrelation;
        }

        [FunctionName("health")]
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
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/health")] HttpRequest request,
            CancellationToken cancellation)
        {
            using (EnrichWithHttpCorrelation())
            {
                try
                {
                    Logger.LogInformation("C# HTTP trigger 'health' function processed a request");
                    if (AcceptsJson(request) == false)
                    {
                        string accept = string.Join(", ", GetAcceptMediaTypes(request));
                        Logger.LogError("Could not process current request because the response could not accept JSON (Accept: {Accept})", accept);

                        return UnsupportedMediaType("Could not process current request because the response could not accept JSON");
                    }

                    HealthReport healthReport = await _healthCheckService.CheckHealthAsync(cancellation);
                    ApiHealthReport apiHealthReport = ApiHealthReport.FromHealthReport(healthReport);

                    if (healthReport?.Status == HealthStatus.Healthy)
                    {
                        return Json(apiHealthReport);
                    }

                    return Json(apiHealthReport, statusCode: StatusCodes.Status503ServiceUnavailable);
                }
                catch (Exception exception)
                {
                    Logger.LogCritical(exception, exception.Message);
                    return InternalServerError("Could not process the current request due to an unexpected exception");
                }
                finally
                {
                    AddCorrelationResponseHeaders(request.HttpContext);
                }
            }
        }
#endif
#if Isolated
        private readonly JsonObjectSerializer _jsonSerializer;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthFunction" /> class.
        /// </summary>
        /// <param name="healthCheckService">The service to check the current health of the running Azure Function.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="healthCheckService"/> is <c>null</c>.</exception>
        public HealthFunction(HealthCheckService healthCheckService, JsonObjectSerializer jsonSerializer)
        {
            Guard.NotNull(healthCheckService, nameof(healthCheckService), "Requires a health check service to check the current health of the running Azure Function");
            Guard.NotNull(jsonSerializer, nameof(jsonSerializer), "Requires a JSON serializer to write JSON contents to the HTTP response");
            
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
#endif
    }
}
