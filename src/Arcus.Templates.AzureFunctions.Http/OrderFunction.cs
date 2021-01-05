using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Arcus.Templates.AzureFunctions.Http.Model;

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
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/order")] HttpRequest request)
        {
            try
            {
                Logger.LogInformation("C# HTTP trigger function processed a request.");
                if (IsJson(request) == false || AcceptsJson(request) == false)
                {
                    string accept = String.Join(", ", GetAcceptMediaTypes(request));
                    Logger.LogError("Could not process current request because the request body is not JSON and/or could not accept JSON as response (Content-Type: {ContentType}, Accept: {Accept})", request.ContentType, accept);
                    
                    return UnsupportedMediaType("Could not process current request because the request body is not JSON and/or could not accept JSON as response");
                }

                if (TryHttpCorrelate(out string errorMessage) == false)
                {
                    return BadRequest(errorMessage);
                }

                var order = await GetJsonBodyAsync<Order>(request);
                return Json(order);
            }
            catch (JsonException exception)
            {
                Logger.LogError(exception, exception.Message);
                return BadRequest("Could not process the current request due to an JSON deserialization failure");
            }
            catch (ValidationException exception)
            {
                Logger.LogError(exception, exception.Message);
                return BadRequest(exception.ValidationResult);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, exception.Message);
                return InternalServerError("Could not process the current request due to an unexpected exception");
            }
        }
    }
}
