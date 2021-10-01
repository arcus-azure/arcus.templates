using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Swagger.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SwaggerDocExamplesTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerDocExamplesTests"/> class.
        /// </summary>
        public SwaggerDocExamplesTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task ExampleProvidersNotIncluded_WithExcludeOpenApiDocs()
        {
            var options = new WebApiProjectOptions().WithExcludeOpenApiDocs();
            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter))
            {
                Assert.False(project.ContainsFile("ExampleProviders\\HealthReportResponseExampleProvider.cs"));
            }
        }

        [Fact]
        public async Task ExampleProvidersIncluded_WithoutExcludeOpenApiDocs()
        {
            var options = new WebApiProjectOptions();
            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter))
            {
                Assert.True(project.ContainsFile("ExampleProviders\\HealthReportResponseExampleProvider.cs"));
            }
        }

        [Fact]
        public async Task GetSwaggerDocs_ReturnsDocsWithHealthEndpointResponseExample()
        {
            // Arrange
            var options = new WebApiProjectOptions();
            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string json = await response.Content.ReadAsStringAsync();
                OpenApiDocument document = LoadOpenApiDocument(json);

                var healthOperation = SelectGetHealthEndpoint(document);
                var okResponse = healthOperation.Responses.Single(r => r.Key == "200").Value;

                var example = SelectHealthPointOkExample(okResponse);

                Assert.Contains("entries", example.Keys);

                var entriesCollection = (OpenApiObject)example["entries"];

                Assert.Contains("api", entriesCollection.Keys);
                Assert.Contains("database", entriesCollection.Keys);
            }
        }

        private static OpenApiDocument LoadOpenApiDocument(string json)
        {
            var reader = new OpenApiStringReader();
            OpenApiDocument document = reader.Read(json, out OpenApiDiagnostic _);

            return document;
        }

        private static OpenApiOperation SelectGetHealthEndpoint(OpenApiDocument document)
        {
            var path = document.Paths.Single(p => p.Key == "/api/v1/health").Value;
            var operation = path.Operations.Single(o => o.Key == OperationType.Get).Value;

            return operation;
        }

        private static OpenApiObject SelectHealthPointOkExample(OpenApiResponse response)
        {
            var example = response.Content.Single(m => m.Key == "application/json").Value.Example;

            return (OpenApiObject)example;
        }
    }
}
