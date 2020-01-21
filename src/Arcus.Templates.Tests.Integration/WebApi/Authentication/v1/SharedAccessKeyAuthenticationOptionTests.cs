using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Authentication.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SharedAccessKeyAuthenticationOptionTests
    {
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationOptionTests"/> class.
        /// </summary>
        public SharedAccessKeyAuthenticationOptionTests(ITestOutputHelper outputWriter)
        {
            _configuration = TestConfig.Create();
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
            
            using (var project = await WebApiProject.StartNewAsync(_configuration, unauthenticatedArguments, _outputWriter))
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
            
            using (var project = await WebApiProject.StartNewAsync(_configuration, authenticatedArguments, _outputWriter))
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
    }
}
