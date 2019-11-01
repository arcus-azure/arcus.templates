using System;
using System.Collections.Generic;
using System.Text;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    public class CannotStartWebApiTemplateProjectException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CannotStartWebApiTemplateProjectException"/> class.
        /// </summary>
        public CannotStartWebApiTemplateProjectException(string message) : base(message)
        {
            
        }
    }
}
