using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web.Http;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using MyProject.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Arcus.Templates.AzureFunctions.Http
{
    public abstract class AzureFunction
    {
        private readonly HttpCorrelation _httpCorrelation;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunction"/> class.
        /// </summary>
        public AzureFunction(HttpCorrelation httpCorrelation, ILogger logger)
        {
            Guard.NotNull(httpCorrelation, nameof(httpCorrelation), "Requires an HTTP correlation instance");
            _httpCorrelation = httpCorrelation;

            Logger = logger;
        }

        static AzureFunction()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                MaxDepth = 10,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None
            };
            jsonSettings.Converters.Add(new StringEnumConverter());
            JsonSerializer = JsonSerializer.Create(jsonSettings);

            var jsonOptions = new JsonSerializerOptions
            {
                MaxDepth = 10,
                IgnoreNullValues = true
            };
            jsonOptions.Converters.Add(new JsonStringEnumConverter());

            OutputFormatters = new FormatterCollection<IOutputFormatter> { new SystemTextJsonOutputFormatter(jsonOptions) };
        }

        protected static JsonSerializer JsonSerializer { get; }

        protected static FormatterCollection<IOutputFormatter> OutputFormatters { get; }

        protected ILogger Logger { get; }

        protected bool TryHttpCorrelate(out string errorMessage)
        {
            return _httpCorrelation.TryHttpCorrelate(out errorMessage);
        }

        protected async Task<TMessage> ReadRequestBodyAsync<TMessage>(HttpRequest request)
        {
            Stream bodyStream = request.BodyReader.AsStream();
            using (var streamReader = new StreamReader(bodyStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                JObject parsedOrder = await JObject.LoadAsync(jsonReader);
                var order = parsedOrder.ToObject<TMessage>(JsonSerializer);

                return order;
            }
        }

        protected bool IsJson(HttpRequest request)
        {
            return request.ContentType == "application/json";
        }

        protected bool AcceptsJson(HttpRequest request)
        {
            return request.GetTypedHeaders().Accept.Any(accept => accept.MediaType == "application/json");
        }

        protected IActionResult Ok(object message)
        {
            return new OkObjectResult(message)
            {
                Formatters = OutputFormatters,
                ContentTypes = { "application/json" }
            };
        }

        protected IActionResult BadRequest(string errorMessage)
        {
            return new BadRequestObjectResult(errorMessage);
        }

        protected ObjectResult StatusCode(int statusCode, object result)
        {
            return new ObjectResult(result)
            {
                StatusCode = statusCode,
                Formatters = OutputFormatters
            };
        }

        protected IActionResult InternalServerError(Exception exception)
        {
            Logger.LogCritical(exception, exception.Message);
            return new InternalServerErrorResult();
        }
    }
}
