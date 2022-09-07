using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.AzureFunctions.Http.Model;
using Bogus;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Logging
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SerilogLoggingTests
    {
        private readonly ITestOutputHelper _outputWriter;

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogLoggingTests" /> class.
        /// </summary>
        public SerilogLoggingTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task HttpTriggerProject_WithoutSerilog_StillProcessHttpRequest()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = BogusGenerator.Random.String(1, 100),
                Scheduled = BogusGenerator.Date.RecentOffset()
            };

            var options = new AzureFunctionsHttpProjectOptions()
                .WithExcludeSerilog();

            // Act
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
                using (HttpResponseMessage response = await project.Order.PostAsync(order))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                string startupContents = project.GetFileContentsInProject("Startup.cs");
                Assert.DoesNotContain("Serilog", startupContents);
            }
        }

        [Fact]
        public async Task HttpTriggerProject_WithSerilog_StillProcessHttpRequest()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = BogusGenerator.Random.String(1, 100),
                Scheduled = BogusGenerator.Date.RecentOffset()
            };

            var options = new AzureFunctionsHttpProjectOptions();

            // Act
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
                using (HttpResponseMessage response = await project.Order.PostAsync(order))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                string startupContents = project.GetFileContentsInProject("Startup.cs");
                Assert.Contains("Serilog", startupContents);
            }
        }
    }
}
