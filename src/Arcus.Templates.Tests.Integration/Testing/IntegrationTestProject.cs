using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Testing;
using Xunit.Abstractions;
using Assert = Xunit.Assert;
using TestConfig = Arcus.Templates.Tests.Integration.Fixture.TestConfig;

namespace Arcus.Templates.Tests.Integration.Testing
{
    /// <summary>
    /// Represents a test fixture implementation of the integration tests project template.
    /// </summary>
    public class IntegrationTestProject : TemplateProject
    {
        private readonly IntegrationTestProjectOptions _options;

        private IntegrationTestProject(
            TestConfig config,
            IntegrationTestProjectOptions options,
            ITestOutputHelper outputWriter)
            : base(config.GetIntegrationTestTemplateProjectDirectory(), 
                   config.GetFixtureProjectDirectory(),
                   outputWriter)
        {
            _options = options;
        }

        /// <summary>
        /// Creates a new integration tests project based on the project template.
        /// </summary>
        /// <param name="options">The additional user options to manipulate the resulting project output.</param>
        /// <param name="outputWriter">The logger instance to write diagnostic messages during the lifetime of the project.</param>
        public static IntegrationTestProject CreateNew(IntegrationTestProjectOptions options, ITestOutputHelper outputWriter)
        {
            var config = TestConfig.Create();
            var project = new IntegrationTestProject(config, options, outputWriter);
            project.CreateNewProject(options);

            return project;
        }

        /// <summary>
        /// Runs the tests in the resulting integration tests project.
        /// </summary>
        public async Task RunTestsAsync()
        {
            RunDotNet($"test {ProjectDirectory.FullName}");

            string fileName = $"{_options.TestFramework.ToString().ToLowerInvariant()}.txt";
            await Poll.Target(() => Task.FromResult(ProjectDirectory.GetFiles(fileName, SearchOption.AllDirectories)))
                      .Until(files => files.Length >= 1)
                      .Every(TimeSpan.FromMilliseconds(100))
                      .Timeout(TimeSpan.FromSeconds(10))
                      .FailWith($"integration test project did not successfully ran the tests with a testing framework '{_options.TestFramework}'");
        }

        /// <summary>
        /// Verifies if the resulting integration tests project does not contain any test framework references except for the provided <paramref name="framework"/>.
        /// </summary>
        public void DoesNotContainOtherTestFrameworksBut(TestFramework framework)
        {
            string csprojContents = GetFileContentsInProject(ProjectName + ".csproj");

            Assert.All(Enum.GetValues<TestFramework>().Except(new[] { framework }), f =>
            {
                Assert.DoesNotContain(f.ToString(), csprojContents, StringComparison.OrdinalIgnoreCase);
            });
        }

        /// <summary>
        /// Verifies if the resulting integration tests project contains a package reference.
        /// </summary>
        public void ContainsPackageReference(TestPackage package)
        {
            string csprojContents = GetFileContentsInProject(ProjectName + ".csproj");
            switch (package)
            {
                case TestPackage.Arcus_Testing_Assert:
                    Assert.Contains("Arcus.Testing.Assert", csprojContents);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(package), package, "Unknown test package");
            }
        }

        /// <summary>
        /// Verifies if the resulting integration tests project does not contain a package reference.
        /// </summary>
        public void DoesNotContainAnyOtherPackageReferencesBut(TestPackage package)
        {
            string csprojContents = GetFileContentsInProject(ProjectName + ".csproj");
            Assert.All(Enum.GetValues<TestPackage>().Except(new[] { package }), p =>
            {
                switch (p)
                {
                    case TestPackage.Arcus_Testing_Assert:
                        Assert.DoesNotContain("Arcus.Testing.Assert", csprojContents);
                        break;
            
                    default:
                        throw new ArgumentOutOfRangeException(nameof(package), package, "Unknown test package");
                }
            });
        }
    }
}
