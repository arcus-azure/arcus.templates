using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Testing.Packaging
{
    public class IncludePackageTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludePackageTests" /> class.
        /// </summary>
        public IncludePackageTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(TestPackage.Arcus_Testing_Assert)]
        public async Task IntegrationTestProject_WithPackage_SucceedsByAddingPackageReference(TestPackage testPackage)
        {
            // Arrange
            var options = new IntegrationTestProjectOptions()
                .WithPackage(testPackage);

            // Act
            using var project = IntegrationTestProject.CreateNew(options, _outputWriter);

            // Assert
            project.ContainsPackageReference(testPackage);
            project.DoesNotContainAnyOtherPackageReferencesBut(testPackage);
            
            await project.RunTestsAsync();
        }
    }
}
