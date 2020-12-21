using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Admin;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Health
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
            AzureFunctionDatabricksConfig databricksConfig = configuration.GetDatabricksConfig();
            var service = new AdminEndpointService(databricksConfig.HttpPort, AzureFunctionsDatabricksProject.FunctionName, _outputWriter);

            // Act / Assert
            await service.TriggerFunctionAsync();
        }
    }
}
