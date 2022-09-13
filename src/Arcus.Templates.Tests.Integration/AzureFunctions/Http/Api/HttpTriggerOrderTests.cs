using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Templates.AzureFunctions.Http.Model;
using Arcus.Templates.Tests.Integration.Fixture;
using Bogus;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Api
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class HttpTriggerOrderTests
    {
        private readonly TestConfig _config;
        private readonly ITestOutputHelper _outputWriter;

        private static readonly Faker BogusGenerator = new Faker();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpTriggerOrderTests"/> class.
        /// </summary>
        public HttpTriggerOrderTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task AzureFunctionsHttpProject_WithoutOptions_ResponseToCorrectOrder()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = BogusGenerator.Random.String(1, 100),
                Scheduled = BogusGenerator.Date.RecentOffset()
            };

            using (var project = await AzureFunctionsHttpProject.StartNewAsync(_config, _outputWriter))
            {
                // Act
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
    }
}
