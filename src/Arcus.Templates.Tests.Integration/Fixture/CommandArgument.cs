using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// 
    /// </summary>
    public class CommandArgument
    {
        private readonly string _name, _value;
        private readonly bool _isSecret;

        private CommandArgument(string name, string value, bool isSecret)
        {
            _name = name;
            _value = value;
            _isSecret = isSecret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal string ToExposedString()
        {
            return $"--{_name} {_value}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static CommandArgument CreateSecret(string name, object value)
        {
            Guard.NotNullOrWhitespace(name, nameof(name), "Name of CLI command argument cannot be blank");
            Guard.NotNull(value, nameof(value), "Value of CLI command argument cannot be 'null'");
            string valueString = value.ToString();
            Guard.NotNullOrWhitespace(valueString, nameof(value), "Value of CLI command argument cannot be blank");

            return new CommandArgument(name, valueString, isSecret: true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static CommandArgument CreateOpen(string name, object value)
        {
            Guard.NotNullOrWhitespace(name, nameof(name), "Name of CLI command argument cannot be blank");
            Guard.NotNull(value, nameof(value), "Value of CLI command argument cannot be 'null'");
            string valueString = value.ToString();
            Guard.NotNullOrWhitespace(valueString, nameof(value), "Value of CLI command argument cannot be blank");

            return new CommandArgument(name, valueString, isSecret: false);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _isSecret ? $"--{_name} ***" : $"--{_name} {_value}";
        }
    }
}
