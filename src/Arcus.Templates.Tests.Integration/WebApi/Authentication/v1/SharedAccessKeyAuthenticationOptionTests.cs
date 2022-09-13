using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Authentication.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SharedAccessKeyAuthenticationOptionTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationOptionTests"/> class.
        /// </summary>
        public SharedAccessKeyAuthenticationOptionTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task SharedAccessKeyAuthenticationOption_GetHealthUnauthenticated_ResultsUnauthorized()
        {
            // Arrange
            const string headerName = "x-shared-access-key";
            const string secretKey = "MySecretKey";
            string secretValue = Guid.NewGuid().ToString("N");

            var unauthenticatedArguments = 
                new WebApiProjectOptions()
                    .WithSharedAccessAuthentication(headerName, secretKey, secretValue);
            
            using (var project = await WebApiProject.StartNewAsync(unauthenticatedArguments, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyAuthenticationOption_GetHealthAuthenticated_ResultsOk()
        {
            // Arrange
            const string headerName = "x-shared-access-key";
            const string secretKey = "MySecretKey";
            string secretValue = Guid.NewGuid().ToString("N");

            var authenticatedArguments = 
                new WebApiProjectOptions()
                    .WithSharedAccessAuthentication(headerName, secretKey, secretValue);
            
            using (var project = await WebApiProject.StartNewAsync(authenticatedArguments, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = 
                    await project.Health.GetAsync(request => request.Headers.Add(headerName, secretValue)))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyAuthenticationOption_GetsSwaggerDocs_ContainsSharedAccessKeySecurityScheme()
        {
            // Arrange
            const string headerName = "x-shared-access-key";
            const string secretKey = "MySecretKey";
            string secretValue = Guid.NewGuid().ToString("N");

            var authenticatedArguments = 
                new WebApiProjectOptions()
                    .WithSharedAccessAuthentication(headerName, secretKey, secretValue);

            using (var project = await WebApiProject.StartNewAsync(authenticatedArguments, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var reader = new OpenApiStreamReader();
                using (Stream json = await response.Content.ReadAsStreamAsync())
                {
                    OpenApiDocument document = reader.Read(json, out OpenApiDiagnostic diagnostic);

                    Assert.NotNull(document.Components);
                    (string schemeName, OpenApiSecurityScheme componentScheme) = Assert.Single(document.Components.SecuritySchemes);
                    Assert.Equal("shared-access-key", schemeName);
                    Assert.Equal(ParameterLocation.Header, componentScheme.In);
                    Assert.Equal(headerName, componentScheme.Name);

                    OpenApiSecurityRequirement requirement = Assert.Single(document.SecurityRequirements);
                    Assert.NotNull(requirement);
                    (OpenApiSecurityScheme requirementScheme, IList<string> scopes) = Assert.Single(requirement);
                    Assert.Equal(headerName, requirementScheme.Name);
                }
            }
        }
    }
}
