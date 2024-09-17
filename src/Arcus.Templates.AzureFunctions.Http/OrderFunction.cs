using System;
using System.Net;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Microsoft.Extensions.Logging;
using Arcus.Templates.AzureFunctions.Http.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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
    public class OrderFunction
    {
        private readonly ISecretProvider _secretProvider;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFunction"/> class.
        /// </summary>
        /// <param name="secretProvider">The instance that provides secrets to the HTTP trigger.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> is <c>null</c>.</exception>
        public OrderFunction(ISecretProvider secretProvider)
        {
            ArgumentNullException.ThrowIfNull(secretProvider);
            _secretProvider = secretProvider;
        }

        [Function("order")]
#if OpenApi
        [OpenApiOperation("Order_Get", tags: new[] { "order" }, Summary = "Gets the order", Description = "Gets the order from the request", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody("application/json", typeof(Order), Description = "The to-be-processed order")]
        [OpenApiParameter("traceparent", In = ParameterLocation.Header, Type = typeof(string), Required = false, Summary = "The correlation header", Description = "The correlation header is used to correlate multiple operation calls")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Order), Summary = "The processed order", Description = "The processed order result", CustomHeaderType = typeof(HttpCorrelationOpenApiResponseHeaders))]
        [OpenApiResponseWithBody(HttpStatusCode.UnsupportedMediaType, "text/plain", typeof(string), Summary = "The faulted response for non-JSON requests", Description = "The faulted response (415) when the request doesn't accept JSON")]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string), Summary = "The faulted response for invalid correlation requests", Description = "The faulted response (400) when the request doesn't correlate correctly")]
        [OpenApiResponseWithBody(HttpStatusCode.InternalServerError, "text/plain", typeof(string), Summary = "The faulted response for general failures", Description = "The faulted response (500) for any general and unexpected server-related failure")]
#endif
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/order")] HttpRequestData request,
            FunctionContext context)
        {
            ILogger logger = context.GetLogger<OrderFunction>();
            logger.LogInformation("C# HTTP trigger 'order' function processed a request");

            var order = await request.ReadFromJsonAsync<Order>();
            HttpResponseData response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(order);

            return response;
        }
    }
}