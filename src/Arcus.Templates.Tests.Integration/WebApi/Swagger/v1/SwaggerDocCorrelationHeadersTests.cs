using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Swagger.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SwaggerDocCorrelationHeadersTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerDocCorrelationHeadersTests"/> class.
        /// </summary>
        public SwaggerDocCorrelationHeadersTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task GetSwaggerDocs_WithExcludeCorrelation_ReturnsDocsWithoutCorrelationHeaders()
        {
            // Arrange
            var options = new WebApiProjectOptions().WithExcludeCorrelation();
            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string json = await response.Content.ReadAsStringAsync();
                OpenApiDocument document = LoadOpenApiDocument(json);

                IDictionary<string, OpenApiHeader> headers = SelectHealthCorrelationResponseHeaders(document);
                Assert.Empty(headers);
                IList<OpenApiParameter> parameters = SelectHealthCorrelationParameters(document);
                Assert.Empty(parameters);
            }
        }

        [Fact]
        public async Task GetSwaggerDocs_WithOpenApiAndCorrelation_ReturnsDocsWithCorrelationHeaders()
        {
            // Arrange
            using (var project = await WebApiProject.StartNewAsync(_outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string json = await response.Content.ReadAsStringAsync();
                OpenApiDocument document = LoadOpenApiDocument(json);

                IDictionary<string, OpenApiHeader> headers = SelectHealthCorrelationResponseHeaders(document);
                Assert.Contains("Request-Id", headers);
                Assert.Contains("X-Transaction-Id", headers);
                IList<OpenApiParameter> parameters = SelectHealthCorrelationParameters(document);
                Assert.Single(parameters, parameter => parameter.Name == "X-Transaction-Id");
            }
        }

        [Fact]
        public async Task GetSwaggerDocs_WithoutOpenApiAndCorrelation_ReturnsNoSwaggerDocs()
        {
            // Arrange
            var options = new WebApiProjectOptions().WithExcludeOpenApiDocs().WithExcludeCorrelation();
            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
            {
                // Assert
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        private static OpenApiDocument LoadOpenApiDocument(string json)
        {
            var reader = new OpenApiStringReader();
            OpenApiDocument document = reader.Read(json, out OpenApiDiagnostic _);
            
            return document;
        }

        private static IDictionary<string, OpenApiHeader> SelectHealthCorrelationResponseHeaders(OpenApiDocument document)
        {
            OpenApiOperation operation = SelectHealthOperation(document);
            (string responseCode, OpenApiResponse okResponse) = Assert.Single(operation.Responses, res => res.Key == "200");
            
            return okResponse.Headers;
        }

        private static IList<OpenApiParameter> SelectHealthCorrelationParameters(OpenApiDocument document)
        {
            OpenApiOperation operation = SelectHealthOperation(document);
            return operation.Parameters;
        }

        private static OpenApiOperation SelectHealthOperation(OpenApiDocument document)
        {
            (string healthPathKey, OpenApiPathItem healthPath) = Assert.Single(document.Paths, path => path.Key == "/api/v1/health");
            (OperationType operationType, OpenApiOperation operation) = Assert.Single(healthPath.Operations, op => op.Key == OperationType.Get);

            return operation;
        }
    }
}
