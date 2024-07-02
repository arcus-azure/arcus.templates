using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Testing.Fixture;
using Bogus;
using Xunit;

namespace Arcus.Templates.Tests.Integration.Testing
{
    /// <summary>
    /// Represents the available test frameworks that can be used in the integration test project template.
    /// </summary>
    public enum TestFramework
    {
        xUnit,
        NUnit,
        MSTest
    }

    /// <summary>
    /// Represents the available test packages that can be included in the integration test project template.
    /// </summary>
    public enum TestPackage
    {
        Arcus_Testing_Assert
    }

    /// <summary>
    /// Represents the available options to configure the integration test project template.
    /// </summary>
    public class IntegrationTestProjectOptions : ProjectOptions
    {
        private readonly Collection<TestPackage> _additionalPackages = new();
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTestProjectOptions"/> class.
        /// </summary>
        public IntegrationTestProjectOptions() : this(Bogus.PickRandom<TestFramework>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTestProjectOptions" /> class.
        /// </summary>
        public IntegrationTestProjectOptions(TestFramework framework)
        {
            WithTestFramework(framework);
        }

        /// <summary>
        /// Gets the configured test framework that will be used in the integration test project.
        /// </summary>
        public TestFramework TestFramework { get; private set; }

        /// <summary>
        /// Gets the additional test packages that will be included in the integration test project.
        /// </summary>
        public IReadOnlyCollection<TestPackage> AdditionalPackages => _additionalPackages;

        /// <summary>
        /// Sets the test framework that will be used in the integration test project.
        /// </summary>
        public IntegrationTestProjectOptions WithTestFramework(TestFramework testFramework)
        {
            TestFramework = testFramework;
            AddOption($"--test-framework {testFramework}",
                (fixturesDir, projectDir) =>
                {
                    string fileName = testFramework switch
                    {
                        TestFramework.xUnit => nameof(XunitTestWriteToDisk),
                        TestFramework.NUnit => nameof(NUnitTestWriteToDisk),
                        TestFramework.MSTest => nameof(MSTestTestWriteToDisk),
                        _ => throw new ArgumentOutOfRangeException(nameof(testFramework), testFramework, null)
                    };

                    FileInfo file = Assert.Single(fixturesDir.EnumerateFiles(fileName + ".cs", SearchOption.AllDirectories));
                    file.CopyTo(Path.Combine(projectDir.FullName, file.Name));
                });

            return this;
        }

        /// <summary>
        /// Adds the specified test package to the integration test project.
        /// </summary>
        public IntegrationTestProjectOptions WithPackage(TestPackage testPackage)
        {
            _additionalPackages.Add(testPackage);
            switch (testPackage)
            {
                case TestPackage.Arcus_Testing_Assert:
                    AddOption("--include-assert-package");
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(testPackage), testPackage, "Unknown test package");
            }

            return this;
        }
    }
}
