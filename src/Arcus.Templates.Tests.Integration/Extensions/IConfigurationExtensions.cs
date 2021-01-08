using System.Collections.Generic;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extensions on the <see cref="IConfiguration"/> instance related to this integration test suite.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IConfigurationExtensions
    {
        /// <summary>
        /// Extracts the required value with the specified <paramref name="key"/> and converts it to type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="configuration">The <see cref="IConfiguration"/> instance to extract the required value from.</param>
        /// <param name="key">The key of the configuration section's value to convert.</param>
        /// <returns>
        ///     The converted value to type <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">Thrown when the <paramref name="key"/> doesn't point to a non-blank configuration value.</exception>
        public static T GetRequiredValue<T>(this IConfiguration configuration, string key)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires an configuration instance to retrieve the required value");
            Guard.NotNullOrWhitespace(key, nameof(key), "Requires a non-blank configuration key to retrieve the required value");

            var value = configuration.GetValue<T>(key);
            if (value is null)
            {
                throw new KeyNotFoundException($"Cannot find non-blank configuration value for key: '{key}'");
            }

            return value;
        }
    }
}
