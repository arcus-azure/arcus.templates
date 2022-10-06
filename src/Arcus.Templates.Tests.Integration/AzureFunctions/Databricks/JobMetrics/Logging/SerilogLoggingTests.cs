using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Logging
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class SerilogLoggingTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogLoggingTests" /> class.
        /// </summary>
        public SerilogLoggingTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task DatabricksProject_WithoutSerilog_StillRuns()
        {
            // Arrange
            var config = TestConfig.Create();
            var options = new AzureFunctionsDatabricksProjectOptions().WithExcludeSerilog();

            // Act
            using (var project = await AzureFunctionsDatabricksProject.StartNewAsync(config, options, _outputWriter))
            {
                // Assert
                await project.Admin.TriggerFunctionAsync();
                Assert.DoesNotContain("Serilog", project.GetFileContentsOfProjectFile());
                Assert.DoesNotContain("Serilog", project.GetFileContentsInProject("Startup.cs"));
            }
        }
    }
}
