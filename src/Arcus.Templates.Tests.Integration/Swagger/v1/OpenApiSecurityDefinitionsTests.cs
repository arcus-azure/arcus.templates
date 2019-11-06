using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

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

                    string swaggerJson = await response.Content.ReadAsStringAsync();
                    JObject swagger = JObject.Parse(swaggerJson);
                    var responses = swagger["paths"][$"/{AuthorizedAndNoneAuthorizedController.AuthorizedRoute}"]["get"]["responses"].Children<JProperty>();
                
                    Assert.Contains(responses, r => r.Name == "401");
                    Assert.Contains(responses, r => r.Name == "403");
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

                    string swaggerJson = await response.Content.ReadAsStringAsync();
                    JObject swagger = JObject.Parse(swaggerJson);
                    var responses = swagger["paths"][$"/{AuthorizedAndNoneAuthorizedController.NoneAuthorizedRoute}"]["get"]["responses"].Children<JProperty>();
                
                    Assert.DoesNotContain(responses, r => r.Name == "401");
                    Assert.DoesNotContain(responses, r => r.Name == "403");
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

                    string swaggerJson = await response.Content.ReadAsStringAsync();
                    JObject swagger = JObject.Parse(swaggerJson);
                    var responses = swagger["paths"][$"/{AuthorizedAndNoneAuthorizedController.AuthorizedRoute}"]["get"]["responses"].Children<JProperty>();

                    Assert.DoesNotContain(responses, r => r.Name == "401");
                    Assert.DoesNotContain(responses, r => r.Name == "403");
                }
            }
        }
    }
}
