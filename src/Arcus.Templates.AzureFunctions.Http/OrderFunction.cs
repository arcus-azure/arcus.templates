using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Security.Core;
using GuardNet;
#if InProcess
using System.ComponentModel.DataAnnotations;
using Arcus.Observability.Telemetry.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http; 
#endif
using Microsoft.Extensions.Logging;
using Arcus.Templates.AzureFunctions.Http.Model;
#if Isolated
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http; 
#endif
#if OpenApi
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models; 
#endif

namespace Arcus.Templates.AzureFunctions.Http
{
    /// <summary>
    /// Represents the root endpoint of the Azure Function.
    /// </summary>
#if InProcess
    public class OrderFunction : HttpBasedAzureFunction 
#endif
#if Isolated
    public class OrderFunction
#endif
    {
        private readonly ISecretProvider _secretProvider;
#if InProcess
        private readonly AzureFunctionsInProcessHttpCorrelation _httpCorrelation;
#endif
#if Isolated
        private readonly JsonObjectSerializer _jsonSerializer;
        private readonly ILogger<OrderFunction> _logger; 
#endif

#if InProcess
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFunction"/> class.
        /// </summary>
        /// <param name="secretProvider">The instance that provides secrets to the HTTP trigger.</param>
        /// <param name="httpCorrelation">The instance to handle the HTTP request correlation.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages while handling the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> or <paramref name="httpCorrelation"/> is <c>null</c>.</exception>
        public OrderFunction(ISecretProvider secretProvider, AzureFunctionsInProcessHttpCorrelation httpCorrelation, ILogger<OrderFunction> logger) : base(logger)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires a secret provider instance");
            _secretProvider = secretProvider;
            _httpCorrelation = httpCorrelation;
        }
#endif
#if Isolated
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFunction" /> class.
        /// </summary>
        /// <param name="secretProvider">The instance that provides secrets to the HTTP trigger.</param>
        /// <param name="jsonSerializer">The common JSON serializer to use across the Azure Functions.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages while handling the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> or the <paramref name="jsonSerializer"/> is <c>null</c>.</exception>
        public OrderFunction(ISecretProvider secretProvider, JsonObjectSerializer jsonSerializer, ILogger<OrderFunction> logger)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires a secret provider instance");
            Guard.NotNull(jsonSerializer, nameof(jsonSerializer), "Requires a JSON serializer instance");
            
            _secretProvider = secretProvider;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }
#endif

#if InProcess
        [FunctionName("order")]
#if OpenApi
        [OpenApiOperation("Order_Get", tags: new[] { "order" }, Summary = "Gets the order", Description = "Gets the order from the request", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody("application/json", typeof(Order), Description = "The to-be-processed order")]
        [OpenApiParameter("traceparent", In = ParameterLocation.Header, Type = typeof(string), Required = false, Summary = "The correlation transaction ID", Description = "The correlation header used to correlate multiple operation calls")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Order), Summary = "The processed order", Description = "The processed order result", CustomHeaderType = typeof(HttpCorrelationOpenApiResponseHeaders))]
        [OpenApiResponseWithBody(HttpStatusCode.UnsupportedMediaType, "text/plain", typeof(string), Summary = "The faulted response for non-JSON requests", Description = "The faulted response (415) when the request doesn't accept JSON")]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string), Summary = "The faulted response for invalid correlation requests", Description = "The faulted response (400) when the request doesn't correlate correctly")]
        [OpenApiResponseWithBody(HttpStatusCode.InternalServerError, "text/plain", typeof(string), Summary = "The faulted response for general failures", Description = "The faulted response (500) for any general and unexpected server-related failure")]
#endif
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/order")] HttpRequest request)
        {
            using (var measurement = DurationMeasurement.Start())
            {
                var statusCode = -1;
                var telemetryContext = new Dictionary<string, object>();

                try
                {
                    Logger.LogInformation("C# HTTP trigger 'order' function processed a request");
                    if (IsJson(request) == false || AcceptsJson(request) == false)
                    {
                        string accept = string.Join(", ", GetAcceptMediaTypes(request));
                        Logger.LogError("Could not process current request because the request body is not JSON and/or could not accept JSON as response (Content-Type: {ContentType}, Accept: {Accept})", request.ContentType, accept);

                        statusCode = StatusCodes.Status415UnsupportedMediaType;
                        return Error(statusCode, "Could not process current request because the request body is not JSON and/or could not accept JSON as response");
                    }

                    var order = await GetJsonBodyAsync<Order>(request);
                    statusCode = StatusCodes.Status200OK;
                    return Json(order, statusCode);
                }
                catch (JsonException exception)
                {
                    Logger.LogError(exception, exception.Message);
                    statusCode = StatusCodes.Status400BadRequest;
                    return Error(statusCode, "Could not process the current request due to an JSON deserialization failure");
                }
                catch (ValidationException exception)
                {
                    Logger.LogError(exception, exception.Message);
                    statusCode = StatusCodes.Status400BadRequest;
                    return Error(statusCode, exception.ValidationResult.ToString());
                }
                catch (Exception exception)
                {
                    Logger.LogCritical(exception, exception.Message);
                    statusCode = StatusCodes.Status500InternalServerError;
                    return Error(statusCode, "Could not process the current request due to an unexpected exception");
                }
                finally
                {
                    _httpCorrelation.AddCorrelationResponseHeaders(request.HttpContext);
                    Logger.LogRequest(request, statusCode, measurement, telemetryContext);
                }
            }
        }
#endif
#if Isolated
        [Function("order")]
#if OpenApi
        [OpenApiOperation("Order_Get", tags: new[] { "order" }, Summary = "Gets the order", Description = "Gets the order from the request", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody("application/json", typeof(Order), Description = "The to-be-processed order")]
        [OpenApiParameter("traceparent", In = ParameterLocation.Header, Type = typeof(string), Required = false, Summary = "The correlation transaction ID", Description = "The correlation header used to correlate multiple operation calls")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Order), Summary = "The processed order", Description = "The processed order result", CustomHeaderType = typeof(HttpCorrelationOpenApiResponseHeaders))]
        [OpenApiResponseWithBody(HttpStatusCode.UnsupportedMediaType, "text/plain", typeof(string), Summary = "The faulted response for non-JSON requests", Description = "The faulted response (415) when the request doesn't accept JSON")]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string), Summary = "The faulted response for invalid correlation requests", Description = "The faulted response (400) when the request doesn't correlate correctly")]
        [OpenApiResponseWithBody(HttpStatusCode.InternalServerError, "text/plain", typeof(string), Summary = "The faulted response for general failures", Description = "The faulted response (500) for any general and unexpected server-related failure")]
#endif
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/order")] HttpRequestData request,
            FunctionContext executionContext)
        {
            try
            {
                _logger.LogInformation("C# HTTP trigger 'order' function processed a request");
                var order = await request.ReadFromJsonAsync<Order>(_jsonSerializer);

                HttpResponseData response = request.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(order, _jsonSerializer);

                return response;
            }
            catch (JsonException exception)
            {
                _logger.LogError(exception, exception.Message);
                HttpResponseData response = request.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync("Could not process the current request due to an JSON deserialization failure");

                return response;
            }
        }
#endif
    }
}