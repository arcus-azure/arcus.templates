using System;
using System.IO;
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

namespace Arcus.Templates.AzureFunctions.Http
{
    /// <summary>
    /// Represents the root endpoint of the Azure Function.
    /// </summary>
    public class HttpFunction
    {
        private readonly ISecretProvider _secretProvider;
        private readonly HttpCorrelation _httpCorrelation;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFunction"/> class.
        /// </summary>
        /// <param name="secretProvider">The instance that provides secrets to the HTTP trigger.</param>
        /// <param name="httpCorrelation">The instance to handle the HTTP request correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> or <paramref name="httpCorrelation"/> is <c>null</c>.</exception>
        public HttpFunction(ISecretProvider secretProvider, HttpCorrelation httpCorrelation)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires a secret provider instance");
            Guard.NotNull(httpCorrelation, nameof(httpCorrelation), "Requires an HTTP correlation instance");
            
            _secretProvider = secretProvider;
            _httpCorrelation = httpCorrelation;
        }

        [FunctionName("http")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest request,
            ILogger logger)
        {
            try
            {
                logger.LogInformation("C# HTTP trigger function processed a request.");

                var formatter = new SystemTextJsonInputFormatter(
                    new JsonOptions(), 
                    NullLogger<SystemTextJsonInputFormatter>.Instance);

                if (request.ContentType != "application/json")
                {
                    return new UnsupportedMediaTypeResult();
                }

                if (_httpCorrelation.TryHttpCorrelate(out string errorMessage) == false)
                {
                    return new BadRequestObjectResult(errorMessage);
                }

                return new OkResult();
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, exception.Message);
                return new InternalServerErrorResult();
            }
        }
    }
}
