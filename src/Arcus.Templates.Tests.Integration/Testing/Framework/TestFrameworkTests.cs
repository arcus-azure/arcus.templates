using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Testing.Framework
{
    public class TestFrameworkTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFrameworkTests" /> class.
        /// </summary>
        public TestFrameworkTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(TestFramework.xUnit)]
        [InlineData(TestFramework.NUnit)]
        [InlineData(TestFramework.MSTest)]
        public async Task IntegrationTestProject_WithTestFramework_SuccessfullyRunTestsWithFramework(TestFramework framework)
        {
            // Arrange
            var options = new IntegrationTestProjectOptions(framework);

            // Act
            using var project = IntegrationTestProject.CreateNew(options, _outputWriter);

            // Assert
            project.DoesNotContainOtherTestFrameworksBut(framework);
            await project.RunTestsAsync();
        }
    }
}
