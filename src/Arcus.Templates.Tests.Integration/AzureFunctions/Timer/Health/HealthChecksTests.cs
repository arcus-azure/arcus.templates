using System.Threading.Tasks;
using Azure.Messaging;
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
            // Arrange
            var options = new AzureFunctionsProjectOptions();
            
            // Act
            await using (var project = await AzureFunctionsTimerProject.StartNewAsync(options, _outputWriter))
            {
                // Assert
                CloudEvent cloudEvent = project.ConsumeTriggeredEvent();
                Assert.NotNull(cloudEvent);
            }
        }
    }
}
