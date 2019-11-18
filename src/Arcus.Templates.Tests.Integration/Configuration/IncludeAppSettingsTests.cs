using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Configuration
{
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AppSettingsFile_GetsIncludedWhen_IncludeAppSettingsProjectOptionIs(bool includeAppSettings)
        {
            // Arrange
            var configuration = TestConfig.Create();
            var projectOptions = new WebApiProjectOptions().WithIncludeAppSettings();

            using (var project = await WebApiProject.StartNewAsync(configuration, projectOptions, _outputWriter))
            {
                
            }
        }
    }
}
