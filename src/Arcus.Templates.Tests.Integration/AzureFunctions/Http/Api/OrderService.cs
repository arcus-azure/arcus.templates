using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Templates.AzureFunctions.Http.Model;
using Arcus.Templates.Tests.Integration.WebApi.Fixture;
using GuardNet;
using Microsoft.Rest;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Api
{
    /// <summary>
    /// HTTP endpoint service to contact the 'Order' HTTP trigger of the Azure Functions test project.
    /// </summary>
    public class OrderService : EndpointService
    {
        private readonly Uri _orderEndpoint;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderService"/> class.
        /// </summary>
        /// <param name="orderEndpoint">The HTTP endpoint where the HTTP trigger for the 'Order' system is located.</param>
        /// <param name="outputWriter">The logger instance to write diagnostic trace messages during the interaction with the 'Order' HTTP trigger of the Azure Functions test project.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="orderEndpoint"/> or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public OrderService(Uri orderEndpoint, ITestOutputHelper outputWriter) : base(outputWriter)
        {
            Guard.NotNull(orderEndpoint, nameof(orderEndpoint), "Requires an HTTP endpoint to locate the 'Order' HTTP trigger in the Azure Functions test project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires an logger instance to write diagnostic trace messages while interacting with the 'Order' HTTP trigger of the Azure Functions test project");
            _orderEndpoint = orderEndpoint;
        }

        /// <summary>
        /// HTTP POST an <paramref name="order"/> to the 'Order' HTTP trigger of the Azure Functions test project.
        /// </summary>
        /// <param name="order">The order to send to the HTTP trigger.</param>
        /// <returns>
        ///     The HTTP response of the HTTP trigger.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="order"/> is <c>null</c>.</exception>
        public async Task<HttpResponseMessage> PostAsync(Order order)
        {
            Guard.NotNull(order, nameof(order), $"Requires an '{nameof(Order)}' model to post to the Azure Functions HTTP trigger");

            string json = JsonSerializer.Serialize(order, JsonOptions);
            HttpResponseMessage response = await PostAsync(json);

            return response;
        }
        
        /// <summary>
        /// HTTP POST an <paramref name="json"/> to the 'Order' HTTP trigger of the Azure Functions test project.
        /// </summary>
        /// <param name="json">The order to send to the HTTP trigger.</param>
        /// <returns>
        ///     The HTTP response of the HTTP trigger.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="json"/> is <c>null</c>.</exception>
        public async Task<HttpResponseMessage> PostAsync(string json)
        {
            Guard.NotNull(json, nameof(json), $"Requires a JSON content representation of an '{nameof(Order)}' model to post to the Azure Function HTTP trigger");
            var content = new StringContent(json);
            
            using (var request = new HttpRequestMessage(HttpMethod.Post, _orderEndpoint))
            {
                request.Content = content;
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                Logger.WriteLine("POST {0} -> {1}", FormatHeaders(request.GetContentHeaders().Concat(request.Headers)), _orderEndpoint);
                HttpResponseMessage response = await HttpClient.SendAsync(request);
                Logger.WriteLine("{0} {1} <- {2}", response.StatusCode, FormatHeaders(response.GetContentHeaders().Concat(response.Headers)), _orderEndpoint);
                
                return response;
            }
        }

        private static string FormatHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            IEnumerable<string> formattedHeaders = headers.Select(header =>
            {
                string values = String.Join(", ", header.Value);
                return $"[{header.Key}] = {values}";
            });

            return $"{{{String.Join(", ", formattedHeaders)}}}";
        }
    }
}
