using System.Threading.Tasks;
using Xunit;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.Health
{
    [Collection(TestCollections.Docker)]
    [Trait("Category", TestTraits.Docker)]
    public class DatabricksHealthDockerTests
    {
        [Fact]
        public Task AzureFunctionsDatabricksProject_WithoutOptions_ShouldAnwserToAdministratorEndpoint()
        {
            // Arrange

            // Act

            // Assert
        }
    }
}
