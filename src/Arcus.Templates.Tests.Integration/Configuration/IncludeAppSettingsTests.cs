using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using static Arcus.Templates.Tests.Integration.Fixture.FeatureToggledController;

namespace Arcus.Templates.Tests.Integration.Configuration
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class IncludeAppSettingsTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeAppSettingsTests"/> class.
        /// </summary>
        public IncludeAppSettingsTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task AppSettingsFile_DoesntGetsLoaded_WhenNoIncludeAppSettingsProjectOptionIsAdded()
        {
            // Arrange
            using (var project = WebApiProject.CreateNew(_outputWriter))
            {
                project.AddFixture<FeatureToggledController>(namespaces: "Controllers");
                await project.StartAsync();

                // Act
                using (HttpResponseMessage response = await project.Root.GetAsync(Route))
                {
                    // Assert
                    Assert.NotNull(response);
                    Assert.False(
                        response.IsSuccessStatusCode,
                        "When no '--include-appsettings' project is added, the feature toggled controller should not response successful");
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AppSettingsFile_GetsLoaded_WhenIncludeAppSettingsProjectOptionIsAdded(bool isToggled)
        {
            // Arrange
            var projectOptions = new WebApiProjectOptions().WithIncludeAppSettings();

            using (var project = WebApiProject.CreateNew(projectOptions, _outputWriter))
            {
                project.AddFixture<FeatureToggledController>(namespaces: "Controllers");
                project.UpdateFileInProject(
                    "appsettings.json", 
                    contents => AddJsonBoolValue(contents, key: FeatureToggle, value: isToggled));

                await project.StartAsync();

                // Act
                using (HttpResponseMessage response = await project.Root.GetAsync(Route))
                {
                    // Assert
                    Assert.NotNull(response);
                    Assert.True(
                        response.IsSuccessStatusCode == isToggled, 
                        "Only when the feature toggle in the 'appsettings.json' is activated, should the controller response successful");
                }
            }
        }

        private static string AddJsonBoolValue(string contents, string key, bool value)
        {
            JObject json = JObject.Parse(contents);
            json[key] = value;
            
            return json.ToString();
        }
    }
}
