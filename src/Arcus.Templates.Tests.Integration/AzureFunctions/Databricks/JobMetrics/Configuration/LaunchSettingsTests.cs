using System.IO;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration
{
    [Trait("Category", TestTraits.Integration)]
    public class LaunchSettingsTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="LaunchSettingsTests" /> class.
        /// </summary>
        public LaunchSettingsTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public void DatabricksJobMetricsTrigger_WithDefault_ConfiguresLaunchSettings()
        {
            // Arrange
            var config = TestConfig.Create();
            var options = new AzureFunctionsDatabricksProjectOptions();

            // Act
            using (var project = AzureFunctionsDatabricksProject.CreateNew(config, options, _outputWriter))
            {
                // Assert
                string relativePath = Path.Combine("Properties", "launchSettings.json");
                string json = project.GetFileContentsInProject(relativePath);
                Assert.Contains(TemplateProject.ProjectName, json);
                Assert.Contains("Docker", json);
            }
        }
    }
}
