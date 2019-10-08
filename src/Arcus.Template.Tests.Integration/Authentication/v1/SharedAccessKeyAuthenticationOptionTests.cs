using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Template.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;
using static Arcus.Template.Tests.Integration.Fixture.InMemorySecretProvider;

namespace Arcus.Template.Tests.Integration.Authentication.v1
{
    [Collection("Integration")]
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
            const string requestHeader = "x-shared-access-key";

            var unauthenticatedArguments = 
                new WebApiProjectOptions()
                    .WithSharedAccessAuthentication(requestHeader, TestSecretKey);
            
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
            const string requestHeader = "x-shared-access-key";

            var authenticatedArguments = 
                new WebApiProjectOptions()
                    .WithSharedAccessAuthentication(requestHeader, TestSecretKey);
            
            using (var project = await WebApiProject.StartNewAsync(_configuration, authenticatedArguments, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = 
                    await project.Health.GetAsync(request => request.Headers.Add(requestHeader, TestSecretValue)))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }
    }
}
