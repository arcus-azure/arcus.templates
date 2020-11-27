using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.Configuration;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.MetricReporting;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.Health
{
    [Collection(TestCollections.Docker)]
    [Trait("Category", TestTraits.Docker)]
    public class DatabricksHealthDockerTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabricksHealthDockerTests"/> class.
        /// </summary>
        public DatabricksHealthDockerTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task AzureFunctionsDatabricksProject_WithoutOptions_ShouldAnswerToAdministratorEndpoint()
        {
            // Arrange
            var configuration = TestConfig.Create();
            AzureFunctionsConfig azureFunctionsConfig = configuration.GetAzureFunctionsConfig();
            var service = new MetricReportingService(azureFunctionsConfig, _outputWriter);

            // Act / Assert
            await service.TriggerFunctionAsync();
        }
    }
}
