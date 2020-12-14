using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using Arcus.Security.Core;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Arcus.Templates.AzureFunctions.Http.Model;

namespace Arcus.Templates.AzureFunctions.Http
{
    /// <summary>
    /// Represents the root endpoint of the Azure Function.
    /// </summary>
    public class OrderFunction : AzureFunction
    {
        private readonly ISecretProvider _secretProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFunction"/> class.
        /// </summary>
        /// <param name="secretProvider">The instance that provides secrets to the HTTP trigger.</param>
        /// <param name="httpCorrelation">The instance to handle the HTTP request correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> or <paramref name="httpCorrelation"/> is <c>null</c>.</exception>
        public OrderFunction(ISecretProvider secretProvider, HttpCorrelation httpCorrelation, ILogger<OrderFunction> logger) : base(httpCorrelation, logger)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires a secret provider instance");
            _secretProvider = secretProvider;
        }

        [FunctionName("order")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest request)
        {
            try
            {
                Logger.LogInformation("C# HTTP trigger function processed a request.");

                if (IsJson(request) == false || AcceptsJson(request) == false)
                {
                    return new UnsupportedMediaTypeResult();
                }

                if (TryHttpCorrelate(out string errorMessage) == false)
                {
                    return new BadRequestObjectResult(errorMessage);
                }

                var order = await ReadRequestBodyAsync<Order>(request);
                return Ok(order);
            }
            catch (Exception exception)
            {
                return InternalServerError(exception);
            }
        }
    }
}
