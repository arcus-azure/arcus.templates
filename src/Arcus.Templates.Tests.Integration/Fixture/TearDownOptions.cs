using System;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// Configuration options to control how the <see cref="TemplateProject"/> implementation should tear down/dispose itself.
    /// Note that these should only be used during development and not kept in the tests permanently.
    /// </summary>
    [Flags]
    public enum TearDownOptions
    {
        /// <summary>
        /// Default, removes all resources created for the <see cref="TemplateProject"/> so the environment is back to it's original state.
        /// </summary>
        None = 0,

        /// <summary>
        /// Keep the temporary created project directory on disk after the <see cref="TemplateProject"/> is disposed.
        /// </summary>
        KeepProjectDirectory = 1,

        /// <summary>
        /// Keep the created project running after the <see cref="TemplateProject"/> is disposed.
        /// </summary>
        KeepProjectRunning = 3,

        /// <summary>
        /// Keep the created project template installed after the <see cref="TemplateProject"/> is disposed.
        /// </summary>
        KeepProjectTemplateInstalled = 4
    }
}
