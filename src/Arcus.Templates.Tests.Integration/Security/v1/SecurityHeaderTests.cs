using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Security.v1
{
    [Collection(TestCollections.Integration)]
    public class SecurityHeaderTests
    {
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        public SecurityHeaderTests(ITestOutputHelper outputWriter)
        {
            _configuration = TestConfig.Create();
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task SecurityHeaders_BasicHttpCall_ServerHeaderNotReturned()
        {
            // Arrange
            using (WebApiProject project = await WebApiProject.StartNewAsync(_configuration, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                var containsAnyServerHeader = response.Headers.Any(header => header.Key.Equals("Server", StringComparison.InvariantCultureIgnoreCase));
                Assert.False(containsAnyServerHeader);
            }
        }
    }
}