using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Bug.EnumParameter
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class EnumsAreAcceptedAsStringsTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumsAreAcceptedAsStringsTests"/> class.
        /// </summary>
        public EnumsAreAcceptedAsStringsTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task GetEnumEndpoint_WithEnumInput_ReturnsOk()
        {
            // Arrange
            using (var project = WebApiProject.CreateNew(_outputWriter))
            {
                project.AddTypeAsFile<EnumController>();
                project.AddTypeAsFile<TestEnum>();
                await project.StartAsync();

                using (var content = new StringContent("\"One\"", Encoding.UTF8, "application/json"))
                using (HttpResponseMessage response = await project.Root.GetAsync(EnumController.GetRoute, content))
                {
                    // Assert
                    Assert.True(HttpStatusCode.OK == response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }
    }
}
