using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;
using static Arcus.Templates.Tests.Integration.Fixture.AuthorizedAndNoneAuthorizedController;

namespace Arcus.Templates.Tests.Integration.Swagger.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class OpenApiSecurityDefinitionsTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiSecurityDefinitionsTests"/> class.
        /// </summary>
        public OpenApiSecurityDefinitionsTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task GetSwaggerDocs_WithOpenApiSecurityDefinitionsOption_IncludeSecurityResponsesOnAuthorizedRoutes()
        {
            // Arrange
            var openApiSecurityDefinitionsOptions =
                new WebApiProjectOptions()
                    .WithOpenApiSecurityDefinitions();
            
            using (var project = WebApiProject.CreateNew(openApiSecurityDefinitionsOptions, _outputWriter))
            {
                project.AddFixture<AuthorizedAndNoneAuthorizedController>();
                await project.StartAsync();

                // Act
                using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    using (Stream swaggerJson = await response.Content.ReadAsStreamAsync())
                    {
                        var streamReader = new OpenApiStreamReader();
                        OpenApiDocument openApiDoc = streamReader.Read(swaggerJson, out OpenApiDiagnostic diagnostic);
                        IEnumerable<string> errors = diagnostic.Errors.Select(err => err.Message);
                        _outputWriter.WriteLine(String.Join(", ", errors));

                        Assert.NotNull(openApiDoc);
                        Assert.NotNull(openApiDoc.Paths);
                        Assert.True(openApiDoc.Paths.TryGetValue($"/{AuthorizedRoute}", out OpenApiPathItem path));
                        Assert.NotNull(path);
                        Assert.NotNull(path.Operations);
                        Assert.True(path.Operations.TryGetValue(OperationType.Get, out OpenApiOperation operation));
                        Assert.NotNull(operation);
                        Assert.NotNull(operation.Responses);
                        Assert.Contains(operation.Responses, r => r.Key == "401");
                        Assert.Contains(operation.Responses, r => r.Key == "403");
                    }
                }
            }
        }

        [Fact]
        public async Task GetSwaggerDocs_WithOpenApiSecurityDefinitionsOption_DoesNotIncludeSecurityResponsesOnNonAuthorizedRoutes()
        {
            // Arrange
            var openApiSecurityDefinitionsOptions =
                new WebApiProjectOptions()
                    .WithOpenApiSecurityDefinitions();

            using (var project = WebApiProject.CreateNew(openApiSecurityDefinitionsOptions, _outputWriter))
            {
                project.AddFixture<AuthorizedAndNoneAuthorizedController>();
                await project.StartAsync();

                // Act
                using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    using (Stream swaggerJson = await response.Content.ReadAsStreamAsync())
                    {
                        var streamReader = new OpenApiStreamReader();
                        OpenApiDocument openApiDoc = streamReader.Read(swaggerJson, out OpenApiDiagnostic diagnostic);
                        IEnumerable<string> errors = diagnostic.Errors.Select(err => err.Message);
                        _outputWriter.WriteLine(String.Join(", ", errors));

                        Assert.NotNull(openApiDoc);
                        Assert.NotNull(openApiDoc.Paths);
                        Assert.True(openApiDoc.Paths.TryGetValue($"/{NoneAuthorizedRoute}", out OpenApiPathItem path));
                        Assert.NotNull(path);
                        Assert.NotNull(path.Operations);
                        Assert.True(path.Operations.TryGetValue(OperationType.Get, out OpenApiOperation operation));
                        Assert.NotNull(operation);
                        Assert.NotNull(operation.Responses);
                        Assert.DoesNotContain(operation.Responses, r => r.Key == "401");
                        Assert.DoesNotContain(operation.Responses, r => r.Key == "403");
                    }
                }
            }
        }

        [Fact]
        public async Task GetSwaggerDocs_WithoutOpenApiSecurityDefinitionsOption_DoesNotIncludeSecurityResponsesOnAuthorizedRoutes()
        {
            // Arrange
            using (var project = WebApiProject.CreateNew(_outputWriter))
            {
                project.AddFixture<AuthorizedAndNoneAuthorizedController>();
                await project.StartAsync();

                // Act
                using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    using (Stream swaggerJson = await response.Content.ReadAsStreamAsync())
                    {
                        var streamReader = new OpenApiStreamReader();
                        OpenApiDocument openApiDoc = streamReader.Read(swaggerJson, out OpenApiDiagnostic diagnostic);
                        IEnumerable<string> errors = diagnostic.Errors.Select(err => err.Message);
                        _outputWriter.WriteLine(String.Join(", ", errors));

                        Assert.NotNull(openApiDoc);
                        Assert.NotNull(openApiDoc.Paths);
                        Assert.True(openApiDoc.Paths.TryGetValue($"/{AuthorizedRoute}", out OpenApiPathItem path));
                        Assert.NotNull(path);
                        Assert.NotNull(path.Operations);
                        Assert.True(path.Operations.TryGetValue(OperationType.Get, out OpenApiOperation operation));
                        Assert.NotNull(operation);
                        Assert.NotNull(operation.Responses);
                        Assert.DoesNotContain(operation.Responses, r => r.Key == "401");
                        Assert.DoesNotContain(operation.Responses, r => r.Key == "403");
                    }
                }
            }
        }
    }
}
