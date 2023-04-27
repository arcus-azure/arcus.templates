using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Timer.Health
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class HealthChecksTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthChecksTests" /> class.
        /// </summary>
        public HealthChecksTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task Timer_WithDefault_Succeeds()
        {
            var options = new AzureFunctionsProjectOptions();
            using (var project = await AzureFunctionsTimerProject.StartNewAsync(options, _outputWriter))
            {
            }
        }
    }
}
