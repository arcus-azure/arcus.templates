using System.IO;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Configuration
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
        public void WebApiTemplate_WithDefault_ConfiguresLaunchSettings()
        {
            // Act
            using (var project = WebApiProject.CreateNew(_outputWriter))
            {
                // Assert
                string relativePath = Path.Combine("Properties", "launchSettings.json");
                string json = project.GetFileContentsInProject(relativePath);
                Assert.Contains(TemplateProject.ProjectName, json);
                Assert.Contains("Docker", json);
                Assert.Contains("api/docs", json);
            }
        }

        [Fact]
        public void WebApiTemplate_WithoutOpenApi_ConfiguresLaunchSettings()
        {
            // Act
            var options = new WebApiProjectOptions().WithExcludeOpenApiDocs();

            // Act
            using (var project = WebApiProject.CreateNew(options, _outputWriter))
            {
                // Assert
                string relativePath = Path.Combine("Properties", "launchSettings.json");
                string json = project.GetFileContentsInProject(relativePath);
                Assert.Contains(TemplateProject.ProjectName, json);
                Assert.Contains("Docker", json);
                Assert.Contains("api/v1/health", json);
            }
        }
    }
}
