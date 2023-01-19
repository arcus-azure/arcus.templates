using System.IO;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus.Configuration
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

        [Theory]
        [InlineData(ServiceBusEntityType.Topic)]
        [InlineData(ServiceBusEntityType.Queue)]
        public void ServiceBusTriggerTemplate_WithDefault_ConfiguresLaunchSettings(ServiceBusEntityType entityType)
        {
            // Arrange
            var options = new AzureFunctionsServiceBusProjectOptions(entityType);
            var config = TestConfig.Create();

            // Act
            using (var project = AzureFunctionsServiceBusProject.CreateNew(entityType, options, config, _outputWriter))
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
