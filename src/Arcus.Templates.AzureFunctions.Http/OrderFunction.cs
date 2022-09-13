using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Core;
using Arcus.Security.Core;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Arcus.Templates.AzureFunctions.Http.Model;
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
    public class OrderFunction : HttpBasedAzureFunction
    {
        private readonly ISecretProvider _secretProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFunction"/> class.
        /// </summary>
        /// <param name="secretProvider">The instance that provides secrets to the HTTP trigger.</param>
        /// <param name="httpCorrelation">The instance to handle the HTTP request correlation.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages while handling the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> or <paramref name="httpCorrelation"/> is <c>null</c>.</exception>
        public OrderFunction(ISecretProvider secretProvider, HttpCorrelation httpCorrelation, ILogger<OrderFunction> logger) : base(httpCorrelation, logger)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires a secret provider instance");
            _secretProvider = secretProvider;
        }

        [FunctionName("order")]
#if OpenApi
        [OpenApiOperation("Order_Get", tags: new[] { "order" }, Summary = "Gets the order", Description = "Gets the order from the request", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody("application/json", typeof(Order), Description = "The to-be-processed order")]
        [OpenApiParameter("X-Transaction-Id", In = ParameterLocation.Header, Type = typeof(string), Required = false, Summary = "The correlation transaction ID", Description = "The correlation transaction ID is used to correlate multiple operation calls")]
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

                    if (TryHttpCorrelate(telemetryContext, out string errorMessage) == false)
                    {
                        statusCode = StatusCodes.Status400BadRequest;
                        return Error(statusCode, errorMessage);
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
                    return Error(statusCode, exception.ValidationResult);
                }
                catch (Exception exception)
                {
                    Logger.LogCritical(exception, exception.Message);
                    statusCode = StatusCodes.Status500InternalServerError;
                    return Error(statusCode, "Could not process the current request due to an unexpected exception");
                }
                finally
                {
                    Logger.LogRequest(request, statusCode, measurement, telemetryContext);
                }
            }
        }
    }
}