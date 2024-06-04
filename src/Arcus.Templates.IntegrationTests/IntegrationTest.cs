using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public abstract class IntegrationTest
    {
#if xUnit
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTest" /> class.
        /// </summary>
        /// <param name="outputWriter">The test output helper to write diagnostic information during the test execution.</param>
        protected IntegrationTest(ITestOutputHelper outputWriter) 
        {
            Config = TestConfig.Create();
            Logger = new XunitTestLogger(outputWriter);
        }
#endif
#if NUnit
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTest" /> class.
        /// </summary>
        protected IntegrationTest()
        {
            Config = TestConfig.Create();
            Logger = new NUnitTestLogger(TestContext.Out, TestContext.Error); 
        }
#elif MSTest
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTest" /> class.
        /// </summary>
        protected IntegrationTest()
        {
            Config = TestConfig.Create();
            Logger = new MSTestLogger(TestContext);
        }

        /// <summary>
        /// Automatically injected test context by the MSTest framework.
        /// </summary>
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
