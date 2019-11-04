using System;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// Exception thrown when a project created from a template cannot be started correctly.
    /// </summary>
    [Serializable]
    public class CannotStartTemplateProject : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CannotStartTemplateProject"/> class.
        /// </summary>
        public CannotStartTemplateProject() : base("The project created from the template cannot be started correctly")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CannotStartTemplateProject"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        public CannotStartTemplateProject(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CannotStartTemplateProject"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CannotStartTemplateProject(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
