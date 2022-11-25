using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Templates.AzureFunctions.Http.Model;
using Arcus.Templates.Tests.Integration.AzureFunctions.Http.Api;
using Arcus.Templates.Tests.Integration.Fixture;
using Bogus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Swagger
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SwaggerOpenApiTests
    {
        private readonly ITestOutputHelper _outputWriter;

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerOpenApiTests" /> class.
        /// </summary>
        public SwaggerOpenApiTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task PostOrder_WithoutOpenApiDocs_StillWorks(FunctionsWorker workerType)
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = BogusGenerator.Random.String(1, 100),
                Scheduled = BogusGenerator.Date.RecentOffset()
            };

            var options =
                new AzureFunctionsHttpProjectOptions()
                    .WithFunctionWorker(workerType)
                    .WithExcludeOpenApiDocs();

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
                using (HttpResponseMessage response = await project.Order.PostAsync(order))
                {
                    // Assert
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Assert.True(HttpStatusCode.OK == response.StatusCode, responseContent);
                    Assert.NotNull(JsonSerializer.Deserialize<Order>(responseContent));

                    IEnumerable<string> responseHeaderNames = response.Headers.Select(header => header.Key).ToArray();
                    Assert.Contains("X-Transaction-ID", responseHeaderNames);
                }
            }
        }

        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task GetHealth_WithoutOpenApiDocs_StillWorks(FunctionsWorker workerType)
        {
            // Arrange
            var options =
                new AzureFunctionsHttpProjectOptions()
                    .WithFunctionWorker(workerType)
                    .WithIncludeHealthChecks()
                    .WithExcludeOpenApiDocs();

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.NotEmpty(response.Headers.GetValues("X-Transaction-Id"));
                    
                    string healthReportJson = await response.Content.ReadAsStringAsync();
                    var healthReport = JObject.Parse(healthReportJson);
                    Assert.Equal(HealthStatus.Healthy.ToString(), healthReport["status"]);
                }
            }
        }

        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task Create_WithoutOpenApiDocs_RemovesOpenApiFiles(FunctionsWorker workerType)
        {
            // Arrange
            var options =
                new AzureFunctionsHttpProjectOptions()
                    .WithFunctionWorker(workerType)
                    .WithExcludeOpenApiDocs();

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
               // Assert
               Assert.False(project.ContainsFile("HttpCorrelationOpenApiResponseHeaders.cs"));
               Assert.False(project.ContainsFile("OpenApiConfigurationOptions.cs"));
            }
        }

        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task Create_WithOpenApiDocs_RemovesOpenApiFiles(FunctionsWorker workerType)
        {
            // Arrange
            var options = new AzureFunctionsHttpProjectOptions().WithFunctionWorker(workerType);
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
                // Assert
                Assert.True(project.ContainsFile("HttpCorrelationOpenApiResponseHeaders.cs"), "should contain OpenApi response headers file");
                Assert.True(project.ContainsFile("OpenApiConfigurationOptions.cs"), "should contain OpenApi configuration options file");
            }
        }
        
        [Theory]
        [InlineData(FunctionsWorker.InProcess, BuildConfiguration.Debug, HttpStatusCode.OK)]
        [InlineData(FunctionsWorker.InProcess, BuildConfiguration.Release, HttpStatusCode.NotFound)]
        public async Task GetSwaggerUI_WithBuildConfiguration_Returns(
            FunctionsWorker workerType, 
            BuildConfiguration buildConfiguration, 
            HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var configuration = TestConfig.Create(buildConfiguration);
            var options = new AzureFunctionsHttpProjectOptions().WithFunctionWorker(workerType);
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(configuration, options, _outputWriter)) 
                // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerUIAsync())
            {
                project.TearDownOptions = TearDownOptions.KeepProjectDirectory;
                // Assert
                Assert.NotNull(response);
                Assert.Equal(expectedStatusCode, response.StatusCode);
            }
        }
        
        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task GetSwaggerUI_WithExcludeOpenApiProjectOption_ReturnsNotFound(FunctionsWorker workerType)
        {
            // Arrange
            var options =
                new AzureFunctionsHttpProjectOptions()
                    .WithFunctionWorker(workerType)
                    .WithExcludeOpenApiDocs();

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
                // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerUIAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task GetSwaggerDocs_WithExcludeOpenApiProjectOption_ReturnsNotFound(FunctionsWorker workerType)
        {
            // Arrange
            var options = 
                new AzureFunctionsHttpProjectOptions()
                    .WithFunctionWorker(workerType)
                    .WithExcludeOpenApiDocs();

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }
        
        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task GetSwaggerDocs_WithOpenApiConfigurationWithoutHealth_ReturnsOpenApiDocumentOfApplication(FunctionsWorker workerType)
        {
            // Arrange
            var options = new AzureFunctionsHttpProjectOptions().WithFunctionWorker(workerType);
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter)) 
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

        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task GetSwaggerDocs_WithOpenApiConfigurationWithHealth_ReturnsOpenApiDocumentOfApplication(FunctionsWorker workerType)
        {
            // Arrange
            var options = 
                new AzureFunctionsHttpProjectOptions()
                    .WithFunctionWorker(workerType)
                    .WithIncludeHealthChecks();
            
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            // Act
            {
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
            Assert.Single(operation.Parameters, parameter => parameter.Name == "traceparent");
            Assert.Collection(operation.Responses,
                res =>
                {
                    Assert.Equal("200", res.Key);
                    Assert.Contains("X-Transaction-Id", res.Value.Headers);
                    Assert.Single(res.Value.Content, content => content.Key == "application/json");
                },
                res => Assert.Equal("415", res.Key),
                res => Assert.Equal("400", res.Key),
                res => Assert.Equal("500", res.Key));
        }
        
        private static void AssertHealthOperation(OpenApiOperation operation)
        {
            Assert.Single(operation.Parameters, parameter => parameter.Name == "traceparent");
            Assert.Collection(operation.Responses,
                res =>
                {
                    Assert.Equal("200", res.Key);
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
