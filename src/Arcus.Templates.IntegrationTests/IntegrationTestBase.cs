using Arcus.Testing;
using Microsoft.Extensions.Logging;
#if xUnit
using Xunit.Abstractions; 
#endif
#if NUnit
using NUnit.Framework; 
#elif MSTest
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Arcus.Templates.IntegrationTests
{
    /// <summary>
    /// Provides a base template for integration tests.
    /// </summary>
    public abstract class IntegrationTestBase
    {
#if xUnit
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTestBase" /> class.
        /// </summary>
        /// <param name="outputWriter">The test output helper to write diagnostic information during the test execution.</param>
        protected IntegrationTestBase(ITestOutputHelper outputWriter) 
        {
            Config = TestConfig.Create();
            Logger = new XunitTestLogger(outputWriter);
        }
#endif
#if NUnit
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTestBase" /> class.
        /// </summary>
        protected IntegrationTestBase()
        {
            Config = TestConfig.Create();
            Logger = new NUnitTestLogger(TestContext.Out, TestContext.Error); 
        }
#elif MSTest
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTestBase" /> class.
        /// </summary>
        protected IntegrationTestBase()
        {
            Config = TestConfig.Create();
            Logger = new MSTestLogger(TestContext);
        }

        /// <summary>
        /// Gets the current context for the test run.
        /// </summary>
        /// <remarks>
        ///     Automatically injected by the MSTest framework.
        /// </remarks>
        public TestContext TestContext { get; set; }
#endif

        /// <summary>
        /// Gets the current loaded application configuration for this test suite.
        /// </summary>
        public TestConfig Config { get; }

        /// <summary>
        /// Gets the logger to write diagnostic information messages during the test execution.
        /// </summary>
        public ILogger Logger { get; }
    }
}
