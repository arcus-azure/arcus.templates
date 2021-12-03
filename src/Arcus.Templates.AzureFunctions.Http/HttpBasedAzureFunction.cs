using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Arcus.Templates.AzureFunctions.Http
{
    /// <summary>
    /// Represents the base HTTP web API functionality for an Azure Function implementation.
    /// </summary>
    public abstract class HttpBasedAzureFunction
    {
        private readonly HttpCorrelation _httpCorrelation;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpBasedAzureFunction"/> class.
        /// </summary>
        /// <param name="httpCorrelation">The correlation service to provide information of related requests.</param>
        /// <param name="logger">The logger instance to write diagnostic messages throughout the execution of the HTTP trigger.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="httpCorrelation"/> is <c>null</c></exception>
        protected HttpBasedAzureFunction(HttpCorrelation httpCorrelation, ILogger logger)
        {
            Guard.NotNull(httpCorrelation, nameof(httpCorrelation), "Requires an HTTP correlation instance");
            _httpCorrelation = httpCorrelation;

            Logger = logger ?? NullLogger.Instance;

            var options = new JsonSerializerOptions();
            options.IgnoreNullValues = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.Converters.Add(new JsonStringEnumConverter());
            JsonOptions = options;
            OutputFormatters = new FormatterCollection<IOutputFormatter> { new SystemTextJsonOutputFormatter(JsonOptions) };
        }

        /// <summary>
        /// Gets the serializer that's being used when incoming requests are being serialized or deserialized into JSON.
        /// </summary>
        protected JsonSerializerOptions JsonOptions { get; }

        /// <summary>
        /// Gets the set of formatters that's being used when an outgoing response is being sent back to the sender.
        /// </summary>
        protected FormatterCollection<IOutputFormatter> OutputFormatters { get; }

        /// <summary>
        /// Gets the logger instance used throughout this Azure Function.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Correlate the current HTTP request according to the previously configured <see cref="T:Arcus.Observability.Correlation.CorrelationInfoOptions" />;
        /// returning an <paramref name="errorMessage" /> when the correlation failed.
        /// </summary>
        /// <param name="errorMessage">The failure message that describes why the correlation of the HTTP request wasn't successful.</param>
        /// <returns>
        ///     <para>[true] when the HTTP request was successfully correlated and the HTTP response was altered accordingly;</para>
        ///     <para>[false] there was a problem with the correlation, describing the failure in the <paramref name="errorMessage" />.</para>
        /// </returns>
        protected bool TryHttpCorrelate(out string errorMessage)
        {
            return _httpCorrelation.TryHttpCorrelate(out errorMessage);
        }

        /// <summary>
        /// Reads the body of the incoming request as JSON to deserialize it into a given <typeparamref name="TMessage"/> type.
        /// </summary>
        /// <typeparam name="TMessage">The type of the request's body that's being deserialized.</typeparam>
        /// <param name="request">The incoming request which body will be deserialized.</param>
        /// <returns>The deserialized request body into the <typeparamref name="TMessage"/> model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c></exception>
        /// <exception cref="JsonReaderException">Thrown when the deserialization of the request body to a JSON representation fails.</exception>
        /// <exception cref="ValidationException">Thrown when the deserialized request body <typeparamref name="TMessage"/> didn't correspond to the required validation rules.</exception>
        protected async Task<TMessage> GetJsonBodyAsync<TMessage>(HttpRequest request)
        {
            TMessage message = await JsonSerializer.DeserializeAsync<TMessage>(request.Body, JsonOptions);
            Validator.ValidateObject(message, new ValidationContext(message), validateAllProperties: true);
            
            return message;
        }

        /// <summary>
        /// Determines whether the incoming <paramref name="request"/>'s body can be considered as JSON or not.
        /// </summary>
        /// <param name="request">The incoming HTTP request to verify.</param>
        /// <returns>
        ///     [true] if the <paramref name="request"/>'s body is considered JSON; [false] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        protected bool IsJson(HttpRequest request)
        {
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to verify if the request's body can be considered as JSON");
            return request.ContentType == "application/json";
        }

        /// <summary>
        /// Determines if the incoming <paramref name="request"/> accepts a JSON response.
        /// </summary>
        /// <param name="request">The incoming HTTP request to verify.</param>
        /// <returns>
        ///     [true] if the incoming <paramref name="request"/> accepts a JSON response; [false] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        protected bool AcceptsJson(HttpRequest request)
        {
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to verify if the request accepts a JSON response");
            return GetAcceptMediaTypes(request).Any(mediaType => mediaType == "application/json");
        }

        /// <summary>
        /// Gets all the media types in the incoming <paramref name="request"/>'s 'Accept' header.
        /// </summary>
        /// <param name="request">The incoming request to extract the media types from..</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        protected IEnumerable<string> GetAcceptMediaTypes(HttpRequest request)
        {
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to retrieve the media types from the Accept header");
            IList<MediaTypeHeaderValue> acceptHeaders = request.GetTypedHeaders().Accept ?? new List<MediaTypeHeaderValue>();
            return acceptHeaders.Select(header => header.MediaType.ToString());
        }

        /// <summary>
        /// Creates an <see cref="IActionResult"/> instance with a JSON response <paramref name="body"/>.
        /// </summary>
        /// <param name="body">The response JSON body.</param>
        /// <param name="statusCode">The HTTP status code of the response; default 200 OK.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="body"/> is <c>null</c>.</exception>
        protected IActionResult Json(object body, int statusCode = StatusCodes.Status200OK)
        {
            Guard.NotNull(body, nameof(body), "Requires a JSON body for the response body");
            return new ObjectResult(body)
            {
                StatusCode = statusCode,
                Formatters = OutputFormatters,
                ContentTypes = { "application/json" }
            };
        }

        /// <summary>
        /// Creates an <see cref="IActionResult"/> instance for a '415 Unsupported Media Type' failure with an accompanied <paramref name="errorMessage"/>.
        /// </summary>
        /// <param name="errorMessage">The error message to describe the failure.</param>
        /// <exception cref="ArgumentException">Throw when the <paramref name="errorMessage"/> is blank.</exception>
        protected IActionResult UnsupportedMediaType(string errorMessage)
        {
            Guard.NotNullOrWhitespace(errorMessage, nameof(errorMessage), "Requires a non-blank error message to accompany the 415 Unsupported Media Type failure");
            return new ObjectResult(errorMessage)
            {
                StatusCode = StatusCodes.Status415UnsupportedMediaType
            };
        }

        /// <summary>
        /// Creates an <see cref="IActionResult"/> instance for a '400 Bad Request' failure with an accompanied <paramref name="error"/> model to describe the failure.
        /// </summary>
        /// <param name="error">The error model to describe the failure.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="error"/> is <c>null</c>.</exception>
        protected IActionResult BadRequest(object error)
        {
            Guard.NotNull(error, nameof(error), "Requires an error model to describe the 400 Bad Request failure");
            return new BadRequestObjectResult(error);
        }

        /// <summary>
        /// Creates an <see cref="IActionResult"/> instance for a '500 Internal Server Error' failure with an accompanied <paramref name="errorMessage"/>.
        /// </summary>
        /// <param name="errorMessage">The error message to describe the failure.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="errorMessage"/> is blank.</exception>
        protected IActionResult InternalServerError(string errorMessage)
        {
            Guard.NotNullOrWhitespace(errorMessage, nameof(errorMessage), "Requires an non-blank error message to accompany the 500 Internal Server Error failure");
            return new ObjectResult(errorMessage)
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                ContentTypes = { "text/plain" }
            };
        }
    }
}
