using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Swagger
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SwaggerOpenApiTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerOpenApiTests" /> class.
        /// </summary>
        public SwaggerOpenApiTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }
        
        [Theory]
        [InlineData(BuildConfiguration.Debug, HttpStatusCode.OK)]
        [InlineData(BuildConfiguration.Release, HttpStatusCode.NotFound)]
        public async Task GetSwaggerUI_WithBuildConfiguration_Returns(BuildConfiguration buildConfiguration, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var configuration = TestConfig.Create(buildConfiguration);
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(configuration, _outputWriter)) 
                // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerUIAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(expectedStatusCode, response.StatusCode);
            }
        }
        
        [Fact]
        public async Task GetSwaggerUI_WithExcludeOpenApiProjectOption_ReturnsNotFound()
        {
            // Arrange
            var options =
                new AzureFunctionsHttpProjectOptions().WithExcludeOpenApiDocs();

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
                // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerUIAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetSwaggerDocs_WithExcludeOpenApiProjectOption_ReturnsNotFound()
        {
            // Arrange
            var options = 
                new AzureFunctionsHttpProjectOptions().WithExcludeOpenApiDocs();

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }
        
        [Fact]
        public async Task GetSwaggerDocs_WithOpenApiConfigurationWithoutHealth_ReturnsOpenApiDocumentOfApplication()
        {
            // Arrange
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(_outputWriter)) 
            // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string json = await response.Content.ReadAsStringAsync();
                OpenApiDocument document = LoadOpenApiDocument(json);
                OpenApiOperation orderOperation = SelectOperation(document, OperationType.Post, "/v1/order");
                AssertOrderOperation(orderOperation);
            }
        }

        [Fact]
        public async Task GetSwaggerDocs_WithOpenApiConfigurationWithHealth_ReturnsOpenApiDocumentOfApplication()
        {
            // Arrange
            var options = new AzureFunctionsHttpProjectOptions().WithIncludeHealthChecks();
            
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            // Act
            {
                project.TearDownOptions = TearDownOptions.KeepProjectDirectory;
                using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string json = await response.Content.ReadAsStringAsync();
                    OpenApiDocument document = LoadOpenApiDocument(json);
                
                    OpenApiOperation orderOperation = SelectOperation(document, OperationType.Post, "/v1/order");
                    AssertOrderOperation(orderOperation);

                    OpenApiOperation healthOperation = SelectOperation(document, OperationType.Get, "/v1/health");
                    AssertHealthOperation(healthOperation);
                }
            }
        }

        private static void AssertOrderOperation(OpenApiOperation operation)
        {
            Assert.Single(operation.Parameters, parameter => parameter.Name == "X-Transaction-Id");
            Assert.Collection(operation.Responses,
                res =>
                {
                    Assert.Equal("200", res.Key);
                    Assert.Contains("RequestId", res.Value.Headers);
                    Assert.Contains("X-Transaction-Id", res.Value.Headers);
                    Assert.Single(res.Value.Content, content => content.Key == "application/json");
                },
                res => Assert.Equal("415", res.Key),
                res => Assert.Equal("400", res.Key),
                res => Assert.Equal("500", res.Key));
        }
        
        private static void AssertHealthOperation(OpenApiOperation operation)
        {
            Assert.Single(operation.Parameters, parameter => parameter.Name == "X-Transaction-Id");
            Assert.Collection(operation.Responses,
                res =>
                {
                    Assert.Equal("200", res.Key);
                    Assert.Contains("RequestId", res.Value.Headers);
                    Assert.Contains("X-Transaction-Id", res.Value.Headers);
                    Assert.Single(res.Value.Content, content => content.Key == "application/json");
                },
                res => Assert.Equal("415", res.Key),
                res => Assert.Equal("400", res.Key),
                res => Assert.Equal("500", res.Key),
                res => Assert.Equal("503", res.Key));
        }

        private static OpenApiDocument LoadOpenApiDocument(string json)
        {
            var reader = new OpenApiStringReader();
            OpenApiDocument document = reader.Read(json, out OpenApiDiagnostic diagnostic);
            
            return document;
        }

        private static OpenApiOperation SelectOperation(OpenApiDocument document, OperationType operationMethod, string operationPath)
        {
            (string healthPathKey, OpenApiPathItem healthPath) = Assert.Single(document.Paths, path => path.Key == operationPath);
            (OperationType operationType, OpenApiOperation operation) = Assert.Single(healthPath.Operations, op => op.Key == operationMethod);

            return operation;
        }
    }
}
