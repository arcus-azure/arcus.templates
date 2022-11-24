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
        [InlineData(FunctionsWorker.Isolated)]
        [InlineData(FunctionsWorker.InProcess)]
        public async Task HttpTriggerProject_WithoutSerilog_StillProcessHttpRequest(FunctionsWorker workerType)
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = BogusGenerator.Random.String(1, 100),
                Scheduled = BogusGenerator.Date.RecentOffset()
            };

            var options = new AzureFunctionsHttpProjectOptions()
                .WithFunctionWorker(workerType)
                .WithExcludeSerilog();

            // Act
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
                using (HttpResponseMessage response = await project.Order.PostAsync(order))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                string runtimeFileName = DetermineRuntimeFile(workerType);
                string startupContents = project.GetFileContentsInProject(runtimeFileName);
                Assert.DoesNotContain("Serilog", startupContents);
            }
        }

        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task HttpTriggerProject_WithSerilog_StillProcessHttpRequest(FunctionsWorker workerType)
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = BogusGenerator.Random.String(1, 100),
                Scheduled = BogusGenerator.Date.RecentOffset()
            };

            var options = new AzureFunctionsHttpProjectOptions().WithFunctionWorker(workerType);

            // Act
            using (var project = await AzureFunctionsHttpProject.StartNewAsync(options, _outputWriter))
            {
                using (HttpResponseMessage response = await project.Order.PostAsync(order))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                string runtimeFileName = DetermineRuntimeFile(workerType);
                string startupContents = project.GetFileContentsInProject(runtimeFileName);
                Assert.Contains("Serilog", startupContents);
            }
        }

        private static string DetermineRuntimeFile(FunctionsWorker workerType)
        {
            switch (workerType)
            {
                case FunctionsWorker.InProcess: return "Startup.cs";
                case FunctionsWorker.Isolated: return "Program.cs";
                default:
                    throw new ArgumentOutOfRangeException(nameof(workerType), workerType, "Unknown functions worker type");
            }
        }
    }
}
