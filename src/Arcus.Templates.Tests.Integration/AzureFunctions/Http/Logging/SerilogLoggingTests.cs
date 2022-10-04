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

        [Theory]
        [InlineData(FunctionsWorker.InProcess, "Startup.cs")]
        [InlineData(FunctionsWorker.Isolated, "Program.cs")]
        public async Task HttpTriggerProject_WithoutSerilog_StillProcessHttpRequest(FunctionsWorker workerType, string startCodeFileName)
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = BogusGenerator.Random.String(1, 100),
                Scheduled = BogusGenerator.Date.RecentOffset()
            };

            var options = new AzureFunctionsHttpProjectOptions()
                .WithFunctionsWorker(workerType)
                .WithExcludeSerilog();

            // Act
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
                using (HttpResponseMessage response = await project.Order.PostAsync(order))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                Assert.DoesNotContain("Serilog", project.GetFileContentsOfProjectFile());
                Assert.DoesNotContain("Serilog", project.GetFileContentsInProject(startCodeFileName));
            }
        }

        [Theory]
        [InlineData(FunctionsWorker.InProcess, "Startup.cs")]
        [InlineData(FunctionsWorker.Isolated, "Program.cs")]
        public async Task HttpTriggerProject_WithSerilog_StillProcessHttpRequest(FunctionsWorker workerType, string startupCodeFileName)
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = BogusGenerator.Random.String(1, 100),
                Scheduled = BogusGenerator.Date.RecentOffset()
            };

            var options = new AzureFunctionsHttpProjectOptions().WithFunctionsWorker(workerType);

            // Act
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
                using (HttpResponseMessage response = await project.Order.PostAsync(order))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                Assert.Contains("Serilog", project.GetFileContentsOfProjectFile());
                Assert.Contains("Serilog", project.GetFileContentsInProject(startupCodeFileName));
            }
        }
    }
}
