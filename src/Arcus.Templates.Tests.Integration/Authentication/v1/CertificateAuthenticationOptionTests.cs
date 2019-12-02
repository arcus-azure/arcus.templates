using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Authentication.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class CertificateAuthenticationOptionTests
    {
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationOptionTests"/> class.
        /// </summary>
        public CertificateAuthenticationOptionTests(ITestOutputHelper outputWriter)
        {
            _configuration = TestConfig.Create();
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

            using (var project = await WebApiProject.StartNewAsync(_configuration, authenticatedProjectArguments, _outputWriter))
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

            using (var project = await WebApiProject.StartNewAsync(_configuration, authenticatedProjectArguments, _outputWriter))
            using (var certificate = SelfSignedCertificate.CreateWithSubject(subject))
            {
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
    }
}
