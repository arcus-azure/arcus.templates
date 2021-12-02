using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.WebApi.Fixture;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Authentication.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class CertificateAuthenticationOptionTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationOptionTests"/> class.
        /// </summary>
        public CertificateAuthenticationOptionTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task CertificateAuthenticationOption_GetHealthUnauthenticated_ResultsUnauthorized()
        {
            // Arrange
            string subject = $"subject-{Guid.NewGuid()}";

            var authenticatedProjectArguments =
                new WebApiProjectOptions()
                    .WithCertificateSubjectAuthentication($"CN={subject}");

            using (var project = await WebApiProject.StartNewAsync(authenticatedProjectArguments, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Health.GetAsync())
                {
                    // Assert
                    Assert.NotNull(response);
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task CertificateAuthenticationOption_GetHealthAuthenticated_ResultsOk()
        {
            // Arrange
            string subject = $"subject-{Guid.NewGuid()}";

            var authenticatedProjectArguments =
                new WebApiProjectOptions()
                    .WithCertificateSubjectAuthentication($"CN={subject}");

            using (var project = await WebApiProject.StartNewAsync(authenticatedProjectArguments, _outputWriter))
            using (var certificate = SelfSignedCertificate.CreateWithSubject(subject))
            {
                project.TearDownOptions = TearDownOptions.KeepProjectDirectory;
                var clientCertificate = Convert.ToBase64String(certificate.RawData);

                // Act
                using (HttpResponseMessage response = 
                    await project.Health.GetAsync(
                        request => request.Headers.Add("X-ARR-ClientCert", clientCertificate)))
                {
                    // Assert
                    Assert.NotNull(response);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task CertificateAuthenticationOption_GetSwaggerDocs_ContainsCertificateSecurityScheme()
        {
            // Arrange
            string subject = $"subject-{Guid.NewGuid()}";

            var authenticatedProjectArguments =
                new WebApiProjectOptions()
                    .WithCertificateSubjectAuthentication($"CN={subject}");

            using (var project = await WebApiProject.StartNewAsync(authenticatedProjectArguments, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var reader = new OpenApiStreamReader();
                    const string headerName = "X-ARR-ClientCert";
                    using (Stream json = await response.Content.ReadAsStreamAsync())
                    {
                        OpenApiDocument document = reader.Read(json, out OpenApiDiagnostic diagnostic);

                        Assert.NotNull(document.Components);
                        (string schemeName, OpenApiSecurityScheme componentScheme) = Assert.Single(document.Components.SecuritySchemes);
                        Assert.Equal("certificate", schemeName);
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
}
